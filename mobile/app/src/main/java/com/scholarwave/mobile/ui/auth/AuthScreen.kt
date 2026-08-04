package com.scholarwave.mobile.ui.auth

import android.content.Context
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
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
    val context = LocalContext.current
    val sharedPrefs = remember { context.getSharedPreferences("auth_prefs", Context.MODE_PRIVATE) }

    val viewModel: AuthViewModel = viewModel { AuthViewModel(AuthRepository()) }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    var isSignUpMode by remember { mutableStateOf(false) }
    var email by remember { mutableStateOf(sharedPrefs.getString("email", "") ?: "") }
    var password by remember { mutableStateOf("") }
    var fullName by remember { mutableStateOf("") }
    var studentClass by remember { mutableStateOf("") }
    var rememberMe by remember { mutableStateOf(sharedPrefs.getBoolean("remember_me", false)) }

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

        if (!isSignUpMode) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.padding(top = 8.dp)
            ) {
                Checkbox(
                    checked = rememberMe,
                    onCheckedChange = { rememberMe = it }
                )
                Spacer(Modifier.width(8.dp))
                Text("Remember me")
            }
        }

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
                if (isSignUpMode) {
                    viewModel.signUp(email, password, fullName, studentClass)
                } else {
                    if (rememberMe) {
                        sharedPrefs.edit()
                            .putString("email", email)
                            .putBoolean("remember_me", true)
                            .apply()
                    } else {
                        sharedPrefs.edit().clear().apply()
                    }
                    viewModel.signIn(email, password)
                }
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