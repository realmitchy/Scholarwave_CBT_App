package com.scholarwave.mobile.ui.results

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.local.QuizSetDao
import com.scholarwave.mobile.data.local.ResultDao
import com.scholarwave.mobile.data.local.ResultEntity
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn

data class ResultWithQuizTitle(
    val result: ResultEntity,
    val quizTitle: String
)

sealed interface ResultsUiState {
    object Loading : ResultsUiState
    data class Success(val results: List<ResultWithQuizTitle>) : ResultsUiState
}

class ResultsViewModel(
    private val resultDao: ResultDao,
    private val quizSetDao: QuizSetDao,
    private val authRepository: AuthRepository
) : ViewModel() {

    private val studentId: String? = authRepository.currentUserId

    val uiState: StateFlow<ResultsUiState> =
        (studentId?.let { id -> resultDao.observeForStudent(id) } ?: flowOf(emptyList()))
            .flatMapLatest { results ->
                if (results.isEmpty()) {
                    flowOf(ResultsUiState.Success(emptyList()) as ResultsUiState)
                } else {
                    quizSetDao.observeAll().map { quizzes ->
                        val titleById = quizzes.associate { it.id to it.title }
                        ResultsUiState.Success(
                            results.map { result ->
                                ResultWithQuizTitle(
                                    result = result,
                                    quizTitle = titleById[result.quizSetId] ?: "Unknown quiz"
                                )
                            }
                        )
                    }
                }
            }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), ResultsUiState.Loading)
}