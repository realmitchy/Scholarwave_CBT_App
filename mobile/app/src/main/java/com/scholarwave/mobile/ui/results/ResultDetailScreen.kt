package com.scholarwave.mobile.ui.results

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.scholarwave.mobile.data.MockExamRepository
import com.scholarwave.mobile.data.local.AppDatabase
import com.scholarwave.mobile.data.local.QuestionEntity
import com.scholarwave.mobile.data.local.QuizSetDao
import com.scholarwave.mobile.data.local.ResultDao
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

// ── UI state ──────────────────────────────────────────────────────────────────

sealed interface ResultDetailUiState {
    object Loading : ResultDetailUiState
    data class Error(val message: String) : ResultDetailUiState
    data class Success(
        val title: String,
        val scoreText: String,
        val sections: List<ReviewSectionUiState>
    ) : ResultDetailUiState
}

data class ReviewSectionUiState(
    val subjectName: String,
    val questions: List<QuestionReviewState>
)

data class QuestionReviewState(
    val text: String,
    val options: List<String>,
    val correctIndex: Int,
    val studentSelectedIndex: Int, // -1 if unanswered
    val explanation: String?
)

// ── ViewModel ─────────────────────────────────────────────────────────────────

class ResultDetailViewModel(
    private val localId: Long,
    private val isMock: Boolean,
    private val resultDao: ResultDao,
    private val quizSetDao: QuizSetDao,
    private val questionDao: com.scholarwave.mobile.data.local.QuestionDao,
    private val mockExamRepository: MockExamRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<ResultDetailUiState>(ResultDetailUiState.Loading)
    val uiState: StateFlow<ResultDetailUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            if (isMock) {
                loadMockResult()
            } else {
                loadPracticeResult()
            }
        }
    }

    private suspend fun loadPracticeResult() {
        val result = resultDao.getById(localId)
        if (result == null) {
            _uiState.value = ResultDetailUiState.Error("Result not found in local database.")
            return
        }

        val quizSet = quizSetDao.getById(result.quizSetId)
        val title = quizSet?.title ?: "Practice Quiz"
        val questions = questionDao.getForQuiz(result.quizSetId)

        val reviews = questions.mapIndexed { index, q ->
            QuestionReviewState(
                text = q.text,
                options = q.options,
                correctIndex = q.correctIndex,
                studentSelectedIndex = result.studentAnswers.getOrElse(index) { -1 },
                explanation = q.explanation
            )
        }

        _uiState.value = ResultDetailUiState.Success(
            title = title,
            scoreText = "Score: ${result.score} / ${result.totalQuestions}",
            sections = listOf(
                ReviewSectionUiState(
                    subjectName = "Practice Section",
                    questions = reviews
                )
            )
        )
    }

    private suspend fun loadMockResult() {
        val detail = mockExamRepository.getMockResultDetail(localId)
        if (detail == null) {
            _uiState.value = ResultDetailUiState.Error("Mock result not found in local database.")
            return
        }

        val sections = detail.sections.map { sec ->
            ReviewSectionUiState(
                subjectName = sec.subjectName,
                questions = sec.questions.mapIndexed { index, q ->
                    QuestionReviewState(
                        text = q.text,
                        options = q.options,
                        correctIndex = q.correctIndex,
                        studentSelectedIndex = sec.studentAnswers.getOrElse(index) { -1 },
                        explanation = q.explanation
                    )
                }
            )
        }

        _uiState.value = ResultDetailUiState.Success(
            title = detail.mockExamTitle,
            scoreText = "Total Score: ${detail.result.totalScore} / ${detail.result.totalQuestions}",
            sections = sections
        )
    }
}

// ── Screen ────────────────────────────────────────────────────────────────────

@Composable
fun ResultDetailScreen(
    localId: Long,
    isMock: Boolean,
    onBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val viewModel: ResultDetailViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        ResultDetailViewModel(
            localId = localId,
            isMock = isMock,
            resultDao = db.resultDao(),
            quizSetDao = db.quizSetDao(),
            questionDao = db.questionDao(),
            mockExamRepository = MockExamRepository(
                mockExamDao = db.mockExamDao(),
                mockExamSectionDao = db.mockExamSectionDao(),
                mockExamResultDao = db.mockExamResultDao(),
                mockExamSectionResultDao = db.mockExamSectionResultDao(),
                subjectDao = db.subjectDao(),
                quizSetDao = db.quizSetDao(),
                questionDao = db.questionDao()
            )
        )
    }

    val state by viewModel.uiState.collectAsStateWithLifecycle()

    Column(modifier.padding(16.dp)) {
        // ── Top Row ───────────────────────────────────────────────────────────
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier.fillMaxWidth()
        ) {
            Button(onClick = onBack) {
                Text("← Back")
            }
            Spacer(Modifier.width(12.dp))
            Text("Review Answers", style = MaterialTheme.typography.headlineSmall)
        }

        Spacer(Modifier.height(16.dp))

        when (val current = state) {
            ResultDetailUiState.Loading -> {
                Text("Loading details…")
            }
            is ResultDetailUiState.Error -> {
                Column {
                    Text(current.message, color = Color.Red)
                    Spacer(Modifier.height(16.dp))
                    Button(onClick = onBack) { Text("Back") }
                }
            }
            is ResultDetailUiState.Success -> {
                ReviewContent(state = current)
            }
        }
    }
}

@Composable
private fun ReviewContent(
    state: ResultDetailUiState.Success,
    modifier: Modifier = Modifier
) {
    var activeSectionIndex by remember { mutableIntStateOf(0) }
    val activeSection = state.sections.getOrNull(activeSectionIndex)

    Column(modifier = modifier) {
        Text(state.title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
        Text(state.scoreText, style = MaterialTheme.typography.bodyLarge)

        Spacer(Modifier.height(12.dp))

        // Tabs if there are multiple sections (like mock exams)
        if (state.sections.size > 1) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                state.sections.forEachIndexed { index, sec ->
                    FilterChip(
                        selected = index == activeSectionIndex,
                        onClick = { activeSectionIndex = index },
                        label = { Text(sec.subjectName) }
                    )
                }
            }
            Spacer(Modifier.height(8.dp))
        }

        HorizontalDivider()
        Spacer(Modifier.height(8.dp))

        if (activeSection == null) {
            Text("No review sections found.")
        } else {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .verticalScroll(rememberScrollState())
            ) {
                activeSection.questions.forEachIndexed { qIdx, question ->
                    QuestionReviewItem(questionNumber = qIdx + 1, review = question)
                    Spacer(Modifier.height(16.dp))
                }
            }
        }
    }
}

@Composable
private fun QuestionReviewItem(
    questionNumber: Int,
    review: QuestionReviewState,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant
        )
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(
                "Question $questionNumber",
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(4.dp))
            Text(review.text, style = MaterialTheme.typography.bodyMedium)
            Spacer(Modifier.height(12.dp))

            val optionLabels = listOf("A", "B", "C", "D")
            review.options.forEachIndexed { optIdx, option ->
                val isCorrectAnswer = optIdx == review.correctIndex
                val isStudentAnswer = optIdx == review.studentSelectedIndex

                val backgroundColor = when {
                    isCorrectAnswer -> Color(0xFFE2F0D9) // Light Green
                    isStudentAnswer -> Color(0xFFFCE4D6) // Light Red/Orange (wrong answer chosen)
                    else -> Color.Transparent
                }

                val borderModifier = if (isStudentAnswer || isCorrectAnswer) {
                    Modifier.background(backgroundColor)
                } else Modifier

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .then(borderModifier)
                        .padding(horizontal = 8.dp, vertical = 4.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "${optionLabels.getOrElse(optIdx) { "$optIdx" }}. $option",
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = if (isCorrectAnswer || isStudentAnswer) FontWeight.Bold else FontWeight.Normal,
                        color = when {
                            isCorrectAnswer -> Color(0xFF385723) // Dark Green text
                            isStudentAnswer -> Color(0xFFC00000) // Dark Red text
                            else -> MaterialTheme.colorScheme.onSurfaceVariant
                        }
                    )
                }
            }

            Spacer(Modifier.height(12.dp))

            // Explanation Section with Placeholder
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surface
                )
            ) {
                Column(Modifier.padding(12.dp)) {
                    Text(
                        "Explanation",
                        style = MaterialTheme.typography.labelMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(Modifier.height(4.dp))
                    val expText = if (!review.explanation.isNullOrBlank()) {
                        review.explanation
                    } else {
                        "No explanation available yet. Detailed solutions are coming soon!"
                    }
                    Text(
                        text = expText,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.7f)
                    )
                }
            }
        }
    }
}
