package com.scholarwave.mobile.ui.exam

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.QuizRepository
import com.scholarwave.mobile.data.ResultsRepository
import com.scholarwave.mobile.data.local.QuestionEntity
import com.scholarwave.mobile.data.local.QuizProgressDao
import com.scholarwave.mobile.data.local.QuizProgressEntity
import com.scholarwave.mobile.data.local.QuizSetEntity
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch

sealed interface ExamUiState {
    object Loading : ExamUiState
    data class Error(val message: String) : ExamUiState
    data class InProgress(
        val quizSet: QuizSetEntity,
        val questions: List<QuestionEntity>,
        val currentIndex: Int,
        val answers: List<Int>, // -1 = unanswered, parallel to questions
        val flagged: List<Boolean>,
        val timeRemainingSeconds: Int
    ) : ExamUiState
    data class Submitted(val score: Int, val total: Int) : ExamUiState
}

class ExamViewModel(
    private val quizSetId: String,
    private val quizRepository: QuizRepository,
    private val resultsRepository: ResultsRepository,
    private val authRepository: AuthRepository,
    private val quizProgressDao: QuizProgressDao
) : ViewModel() {

    private val _uiState = MutableStateFlow<ExamUiState>(ExamUiState.Loading)
    val uiState: StateFlow<ExamUiState> = _uiState.asStateFlow()

    private var timerJob: Job? = null
    private var studentId: String = ""
    private var studentName: String = ""
    private var studentClass: String = ""

    init {
        viewModelScope.launch {
            val quizSet = quizRepository.getQuizSet(quizSetId)
            val questions = quizRepository.observeQuestions(quizSetId).first()
            val profile = authRepository.getMyProfile()

            if (quizSet == null || questions.isEmpty() || profile == null) {
                _uiState.value = ExamUiState.Error(
                    "Couldn't load this exam. Check your connection and try again."
                )
                return@launch
            }

            studentId = profile.id
            studentName = profile.fullName
            studentClass = profile.studentClass ?: ""

            val saved = quizProgressDao.get(quizSetId)

            val currentIndex: Int
            val answers: List<Int>
            val flagged: List<Boolean>
            val timeRemaining: Int

            if (saved != null) {
                currentIndex = saved.currentIndex
                answers = saved.studentAnswers.toList()
                flagged = saved.flagged.toList()
                timeRemaining = saved.timeRemainingSeconds
            } else {
                currentIndex = 0
                answers = List(questions.size) { -1 }
                flagged = List(questions.size) { false }
                timeRemaining = quizSet.timeLimitMinutes * 60
            }

            _uiState.value = ExamUiState.InProgress(
                quizSet = quizSet,
                questions = questions,
                currentIndex = currentIndex,
                answers = answers,
                flagged = flagged,
                timeRemainingSeconds = timeRemaining
            )
            startTimer()
        }
    }

    private fun startTimer() {
        timerJob?.cancel()
        timerJob = viewModelScope.launch {
            while (true) {
                delay(1000)
                val state = _uiState.value as? ExamUiState.InProgress ?: return@launch
                val newTime = state.timeRemainingSeconds - 1
                if (newTime <= 0) {
                    _uiState.value = state.copy(timeRemainingSeconds = 0)
                    submit()
                    return@launch
                }
                _uiState.value = state.copy(timeRemainingSeconds = newTime)
                if (newTime % 5 == 0) saveProgress()
            }
        }
    }

    fun selectAnswer(optionIndex: Int) {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        val updatedAnswers = state.answers.toMutableList()
        updatedAnswers[state.currentIndex] = optionIndex
        _uiState.value = state.copy(answers = updatedAnswers)
        saveProgress()
    }

    fun toggleFlag() {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        val updatedFlags = state.flagged.toMutableList()
        updatedFlags[state.currentIndex] = !updatedFlags[state.currentIndex]
        _uiState.value = state.copy(flagged = updatedFlags)
        saveProgress()
    }

    fun goToQuestion(index: Int) {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        if (index in state.questions.indices) {
            _uiState.value = state.copy(currentIndex = index)
            saveProgress()
        }
    }

    fun nextQuestion() {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        goToQuestion(state.currentIndex + 1)
    }

    fun previousQuestion() {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        goToQuestion(state.currentIndex - 1)
    }

    private fun saveProgress() {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        viewModelScope.launch {
            quizProgressDao.save(
                QuizProgressEntity(
                    quizSetId = quizSetId,
                    studentName = studentName,
                    studentClass = studentClass,
                    timeRemainingSeconds = state.timeRemainingSeconds,
                    currentIndex = state.currentIndex,
                    studentAnswers = state.answers.toIntArray(),
                    flagged = state.flagged.toBooleanArray()
                )
            )
        }
    }

    fun submit() {
        val state = _uiState.value as? ExamUiState.InProgress ?: return
        timerJob?.cancel()
        viewModelScope.launch {
            val score = state.questions.indices.count { i ->
                state.answers[i] == state.questions[i].correctIndex
            }
            resultsRepository.submitResult(
                quizSetId = quizSetId,
                studentId = studentId,
                studentName = studentName,
                studentClass = studentClass,
                score = score,
                totalQuestions = state.questions.size
            )
            quizProgressDao.delete(quizSetId)
            _uiState.value = ExamUiState.Submitted(score, state.questions.size)
        }
    }

    fun retake() {
        viewModelScope.launch {
            val quizSet = quizRepository.getQuizSet(quizSetId) ?: return@launch
            val questions = quizRepository.observeQuestions(quizSetId).first()
            if (questions.isEmpty()) return@launch

            _uiState.value = ExamUiState.InProgress(
                quizSet = quizSet,
                questions = questions,
                currentIndex = 0,
                answers = List(questions.size) { -1 },
                flagged = List(questions.size) { false },
                timeRemainingSeconds = quizSet.timeLimitMinutes * 60
            )
            startTimer()
        }
    }

    override fun onCleared() {
        super.onCleared()
        timerJob?.cancel()
    }
}