package com.scholarwave.mobile.ui.results

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.local.QuizSetDao
import com.scholarwave.mobile.data.local.ResultDao
import com.scholarwave.mobile.data.local.MockExamDao
import com.scholarwave.mobile.data.local.MockExamResultDao
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.flow.stateIn

data class DisplayResult(
    val localId: Long,
    val title: String,
    val score: Int,
    val totalQuestions: Int,
    val submittedAtEpochMillis: Long,
    val isMock: Boolean,
    val synced: Boolean
)

sealed interface ResultsUiState {
    object Loading : ResultsUiState
    data class Success(val results: List<DisplayResult>) : ResultsUiState
}

class ResultsViewModel(
    private val resultDao: ResultDao,
    private val quizSetDao: QuizSetDao,
    private val mockExamResultDao: MockExamResultDao,
    private val mockExamDao: MockExamDao,
    private val authRepository: AuthRepository
) : ViewModel() {

    private val studentId: String? = authRepository.currentUserId

    val uiState: StateFlow<ResultsUiState> =
        if (studentId == null) {
            flowOf(ResultsUiState.Success(emptyList()))
        } else {
            combine(
                resultDao.observeForStudent(studentId),
                mockExamResultDao.observeForStudent(studentId),
                quizSetDao.observeAll(),
                mockExamDao.observePublished()
            ) { practiceResults, mockResults, quizzes, mockExams ->
                val quizTitleById = quizzes.associate { it.id to it.title }
                val mockTitleById = mockExams.associate { it.id to it.title }

                val displayPractice = practiceResults.map { r ->
                    DisplayResult(
                        localId = r.localId,
                        title = quizTitleById[r.quizSetId] ?: "Practice Quiz",
                        score = r.score,
                        totalQuestions = r.totalQuestions,
                        submittedAtEpochMillis = r.submittedAtEpochMillis,
                        isMock = false,
                        synced = r.synced
                    )
                }

                val displayMock = mockResults.map { r ->
                    DisplayResult(
                        localId = r.localId,
                        title = mockTitleById[r.mockExamId] ?: "Mock Exam",
                        score = r.totalScore,
                        totalQuestions = r.totalQuestions,
                        submittedAtEpochMillis = r.submittedAtEpochMillis,
                        isMock = true,
                        synced = r.synced
                    )
                }

                val combined = (displayPractice + displayMock).sortedByDescending { it.submittedAtEpochMillis }
                ResultsUiState.Success(combined)
            }
        }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), ResultsUiState.Loading)
}