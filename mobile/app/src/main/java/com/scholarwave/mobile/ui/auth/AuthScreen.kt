package com.scholarwave.mobile.ui.auth

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.scholarwave.mobile.data.AuthRepository

@Composable
fun AuthScreen(
    onLoggedIn: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val viewModel: AuthViewModel = viewModel { AuthViewModel(AuthRepository()) }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    var isSignUpMode by remember { mutableStateOf(false) }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var fullName by remember { mutableStateOf("") }
    var studentClass by remember { mutableStateOf("") }

    if (state is AuthUiState.LoggedIn) {
        onLoggedIn()
        return
    }

    Column(modifier.padding(24.dp)) {
        Text(if (isSignUpMode) "Create Student Account" else "Student Login")

        OutlinedTextField(
            value = email, onValueChange = { email = it },
            label = { Text("Email") }, modifier = Modifier.fillMaxWidth().padding(top = 16.dp)
        )
        OutlinedTextField(
            value = password, onValueChange = { password = it },
            label = { Text("Password") }, modifier = Modifier.fillMaxWidth().padding(top = 8.dp)
        )

        if (isSignUpMode) {
            OutlinedTextField(
                value = fullName, onValueChange = { fullName = it },
                label = { Text("Full Name") }, modifier = Modifier.fillMaxWidth().padding(top = 8.dp)
            )
            OutlinedTextField(
                value = studentClass, onValueChange = { studentClass = it },
                label = { Text("Class") }, modifier = Modifier.fillMaxWidth().padding(top = 8.dp)
            )
        }

        Button(
            onClick = {
                if (isSignUpMode) viewModel.signUp(email, password, fullName, studentClass)
                else viewModel.signIn(email, password)
            },
            modifier = Modifier.fillMaxWidth().padding(top = 16.dp)
        ) {
            Text(if (isSignUpMode) "Sign Up" else "Log In")
        }

        TextButton(onClick = { isSignUpMode = !isSignUpMode }) {
            Text(if (isSignUpMode) "Already have an account? Log in" else "New student? Sign up")
        }

        when (val s = state) {
            is AuthUiState.Error -> Text("Error: ${s.message}")
            AuthUiState.CheckEmail -> Text("Account created — please check your email to confirm before logging in.")
            AuthUiState.Loading -> Text("Please wait...")
            else -> {}
        }
    }
}