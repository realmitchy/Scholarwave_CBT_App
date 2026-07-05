package com.scholarwave.mobile.ui.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.QuizRepository
import com.scholarwave.mobile.data.local.QuizSetEntity
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class MainScreenViewModel(private val quizRepository: QuizRepository) : ViewModel() {

    val uiState: StateFlow<MainScreenUiState> =
        quizRepository.observePublishedQuizzes()
            .map<List<QuizSetEntity>, MainScreenUiState>(MainScreenUiState::Success)
            .catch { emit(MainScreenUiState.Error(it)) }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), MainScreenUiState.Loading)

    fun refresh() {
        viewModelScope.launch {
            runCatching { quizRepository.refreshFromCloud() }
        }
    }
}

sealed interface MainScreenUiState {
    object Loading : MainScreenUiState
    data class Error(val throwable: Throwable) : MainScreenUiState
    data class Success(val data: List<QuizSetEntity>) : MainScreenUiState
}