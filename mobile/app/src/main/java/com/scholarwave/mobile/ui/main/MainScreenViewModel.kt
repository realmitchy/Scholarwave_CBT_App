package com.scholarwave.mobile.ui.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.MockExamRepository
import com.scholarwave.mobile.data.local.MockExamEntity
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class MainScreenViewModel(private val mockExamRepository: MockExamRepository) : ViewModel() {

    val uiState: StateFlow<MainScreenUiState> =
        mockExamRepository.observePublishedMockExams()
            .map<List<MockExamEntity>, MainScreenUiState>(MainScreenUiState::Success)
            .catch { emit(MainScreenUiState.Error(it)) }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), MainScreenUiState.Loading)

    fun refresh() {
        viewModelScope.launch {
            runCatching { mockExamRepository.refreshFromCloud() }
        }
    }
}

sealed interface MainScreenUiState {
    object Loading : MainScreenUiState
    data class Error(val throwable: Throwable) : MainScreenUiState
    data class Success(val data: List<MockExamEntity>) : MainScreenUiState
}