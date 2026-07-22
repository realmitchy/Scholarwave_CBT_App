package com.scholarwave.mobile.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.scholarwave.mobile.data.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

sealed interface AuthUiState {
    object Idle : AuthUiState
    object Loading : AuthUiState
    object CheckEmail : AuthUiState
    object LoggedIn : AuthUiState
    data class Error(val message: String) : AuthUiState
}

class AuthViewModel(private val authRepository: AuthRepository) : ViewModel() {
    private val _uiState = MutableStateFlow<AuthUiState>(AuthUiState.Idle)
    val uiState: StateFlow<AuthUiState> = _uiState.asStateFlow()

    fun signUp(email: String, password: String, fullName: String, studentClass: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            try {
                val loggedInImmediately = authRepository.signUpStudent(email, password, fullName, studentClass)
                _uiState.value = if (loggedInImmediately) AuthUiState.LoggedIn else AuthUiState.CheckEmail
            } catch (e: Exception) {
                _uiState.value = AuthUiState.Error(e.message ?: "Sign up failed")
            }
        }
    }

    fun signIn(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            try {
                authRepository.signIn(email, password)
                authRepository.ensureProfileExists(fullName = "", studentClass = "")
                _uiState.value = AuthUiState.LoggedIn
            } catch (e: Exception) {
                _uiState.value = AuthUiState.Error(e.message ?: "Login failed")
            }
        }
    }
}