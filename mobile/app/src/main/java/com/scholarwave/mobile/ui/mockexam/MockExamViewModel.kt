package com.scholarwave.mobile.ui.mockexam

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.MockExamRepository
import com.scholarwave.mobile.data.MockExamSection
import com.scholarwave.mobile.data.MockExamWithSections
import com.scholarwave.mobile.data.SectionScore
import com.scholarwave.mobile.data.local.QuestionEntity
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

// ── UI state ──────────────────────────────────────────────────────────────────

sealed interface MockExamUiState {
    object Loading : MockExamUiState
    data class Error(val message: String) : MockExamUiState
    data class InProgress(
        val mockExamTitle: String,
        val sections: List<SectionUiState>,
        val activeSectionIndex: Int,         // which tab is active (0–3)
        val currentQuestionIndex: Int,       // index within the active section
        val timeRemainingSeconds: Int        // ONE combined timer for the whole sitting
    ) : MockExamUiState
    data class Submitted(
        val totalScore: Int,
        val totalQuestions: Int,
        val sectionScores: List<SectionScoreUi>
    ) : MockExamUiState
}

/**
 * Per-section state held inside [MockExamUiState.InProgress].
 */
data class SectionUiState(
    val subjectName: String,
    val subjectCode: String,
    val questions: List<QuestionEntity>,
    val answers: List<Int>,          // parallel to questions; -1 = unanswered
    val flagged: List<Boolean>
) {
    val answeredCount: Int get() = answers.count { it >= 0 }
}

/** Result row shown on the Submitted screen. */
data class SectionScoreUi(
    val subjectName: String,
    val score: Int,
    val total: Int
)

// ── ViewModel ─────────────────────────────────────────────────────────────────

class MockExamViewModel(
    private val mockExamId: String,
    private val mockExamRepository: MockExamRepository,
    private val authRepository: AuthRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<MockExamUiState>(MockExamUiState.Loading)
    val uiState: StateFlow<MockExamUiState> = _uiState.asStateFlow()

    private var timerJob: Job? = null
    private var studentId: String = ""

    init {
        viewModelScope.launch {
            val profile = authRepository.getMyProfile()
            if (profile == null) {
                _uiState.value = MockExamUiState.Error("Could not load your profile. Please sign in again.")
                return@launch
            }
            studentId = profile.id

            // Ensure sections and questions are fully loaded/synced from cloud before reading from Room
            runCatching { mockExamRepository.refreshFromCloud() }

            val data = mockExamRepository.getMockExamWithSections(mockExamId)
            if (data == null || data.sections.isEmpty()) {
                _uiState.value = MockExamUiState.Error(
                    "Could not load this mock exam. Make sure you're connected and try again."
                )
                return@launch
            }

            _uiState.value = MockExamUiState.InProgress(
                mockExamTitle = data.exam.title,
                sections = data.sections.map { it.toUiState() },
                activeSectionIndex = 0,
                currentQuestionIndex = 0,
                timeRemainingSeconds = data.exam.durationMinutes * 60
            )
            startTimer()
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private fun startTimer() {
        timerJob?.cancel()
        timerJob = viewModelScope.launch {
            while (true) {
                delay(1000)
                val state = _uiState.value as? MockExamUiState.InProgress ?: return@launch
                val newTime = state.timeRemainingSeconds - 1
                if (newTime <= 0) {
                    _uiState.value = state.copy(timeRemainingSeconds = 0)
                    submit()
                    return@launch
                }
                _uiState.value = state.copy(timeRemainingSeconds = newTime)
            }
        }
    }

    // ── Navigation within the exam ────────────────────────────────────────────

    /** Switch to a different subject tab. Remembers the last question index per section. */
    fun switchSection(index: Int) {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        if (index !in state.sections.indices) return
        _uiState.value = state.copy(
            activeSectionIndex = index,
            currentQuestionIndex = 0   // always start at Q1 when switching sections
        )
    }

    fun goToQuestion(index: Int) {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        val section = state.sections[state.activeSectionIndex]
        if (index in section.questions.indices) {
            _uiState.value = state.copy(currentQuestionIndex = index)
        }
    }

    fun nextQuestion() {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        goToQuestion(state.currentQuestionIndex + 1)
    }

    fun previousQuestion() {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        goToQuestion(state.currentQuestionIndex - 1)
    }

    // ── Answer + flag ─────────────────────────────────────────────────────────

    fun selectAnswer(optionIndex: Int) {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        val sections = state.sections.toMutableList()
        val section = sections[state.activeSectionIndex]
        val updatedAnswers = section.answers.toMutableList()
        updatedAnswers[state.currentQuestionIndex] = optionIndex
        sections[state.activeSectionIndex] = section.copy(answers = updatedAnswers)
        _uiState.value = state.copy(sections = sections)
    }

    fun toggleFlag() {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        val sections = state.sections.toMutableList()
        val section = sections[state.activeSectionIndex]
        val updatedFlags = section.flagged.toMutableList()
        updatedFlags[state.currentQuestionIndex] = !updatedFlags[state.currentQuestionIndex]
        sections[state.activeSectionIndex] = section.copy(flagged = updatedFlags)
        _uiState.value = state.copy(sections = sections)
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    fun submit() {
        val state = _uiState.value as? MockExamUiState.InProgress ?: return
        timerJob?.cancel()

        viewModelScope.launch {
            val sectionScores = state.sections.map { section ->
                val score = section.questions.indices.count { i ->
                    section.answers[i] == section.questions[i].correctIndex
                }
                SectionScore(
                    // subjectId not critical for local display; use empty string as placeholder
                    // (the real subjectId is in MockExamSection, not SectionUiState)
                    subjectId = "",
                    subjectName = section.subjectName,
                    score = score,
                    totalQuestions = section.questions.size,
                    studentAnswers = section.answers.toIntArray()
                )
            }

            val totalScore = sectionScores.sumOf { it.score }
            val totalQuestions = sectionScores.sumOf { it.totalQuestions }

            // Fetch subjectIds from Room for the real sync payload
            val dataWithIds = mockExamRepository.getMockExamWithSections(mockExamId)
            val enrichedScores = if (dataWithIds != null) {
                state.sections.mapIndexed { i, section ->
                    val sectionEntity = dataWithIds.sections.getOrNull(i)
                    val score = section.questions.indices.count { qi ->
                        section.answers[qi] == section.questions[qi].correctIndex
                    }
                    SectionScore(
                        subjectId = sectionEntity?.sectionEntity?.subjectId ?: "",
                        subjectName = section.subjectName,
                        score = score,
                        totalQuestions = section.questions.size,
                        studentAnswers = section.answers.toIntArray()
                    )
                }
            } else sectionScores

            mockExamRepository.submitMockResult(
                mockExamId = mockExamId,
                studentId = studentId,
                totalScore = totalScore,
                totalQuestions = totalQuestions,
                sectionScores = enrichedScores
            )

            _uiState.value = MockExamUiState.Submitted(
                totalScore = totalScore,
                totalQuestions = totalQuestions,
                sectionScores = enrichedScores.map { s ->
                    SectionScoreUi(
                        subjectName = s.subjectName,
                        score = s.score,
                        total = s.totalQuestions
                    )
                }
            )
        }
    }

    override fun onCleared() {
        super.onCleared()
        timerJob?.cancel()
    }
}

// ── Mapping helper ────────────────────────────────────────────────────────────

private fun MockExamSection.toUiState() = SectionUiState(
    subjectName = subjectName,
    subjectCode = subjectCode,
    questions = questions,
    answers = List(questions.size) { -1 },
    flagged = List(questions.size) { false }
)
