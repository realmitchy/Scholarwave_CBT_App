package com.scholarwave.mobile.ui.main

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation3.runtime.NavKey
import com.scholarwave.mobile.Exam
import com.scholarwave.mobile.Results
import com.scholarwave.mobile.data.QuizRepository
import com.scholarwave.mobile.data.local.AppDatabase
import com.scholarwave.mobile.data.local.QuizSetEntity
import com.scholarwave.mobile.theme.ScholarwaveMobileTheme

@Composable
fun MainScreen(
    onItemClick: (NavKey) -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val viewModel: MainScreenViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        MainScreenViewModel(QuizRepository(db.quizSetDao(), db.questionDao()))
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    LaunchedEffect(Unit) { viewModel.refresh() }

    Column(modifier.padding(16.dp)) {
        Button(onClick = { onItemClick(Results) }) {
            Text("My Results")
        }

        when (state) {
            MainScreenUiState.Loading -> Text("Loading quizzes...", modifier = Modifier.padding(top = 16.dp))
            is MainScreenUiState.Success -> {
                QuizListContent(
                    quizzes = (state as MainScreenUiState.Success).data,
                    onQuizClick = { quiz -> onItemClick(Exam(quiz.id)) },
                )
            }
            is MainScreenUiState.Error -> {
                Text(
                    "Couldn't refresh — showing cached quizzes if any. (${(state as MainScreenUiState.Error).throwable.message})",
                    modifier = Modifier.padding(top = 16.dp)
                )
            }
        }
    }
}

@Composable
internal fun QuizListContent(
    quizzes: List<QuizSetEntity>,
    onQuizClick: (QuizSetEntity) -> Unit = {},
    modifier: Modifier = Modifier
) {
    Column(modifier.padding(top = 16.dp)) {
        Text("Available Quizzes")
        if (quizzes.isEmpty()) {
            Text("No quizzes yet — publish one from the Supabase Table Editor to test.")
        } else {
            quizzes.forEach { quiz ->
                Text(
                    "• ${quiz.title} (${quiz.timeLimitMinutes} min)",
                    modifier = Modifier
                        .clickable { onQuizClick(quiz) }
                        .padding(vertical = 8.dp)
                )
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
fun MainScreenPreview() {
    ScholarwaveMobileTheme {
        QuizListContent(
            listOf(QuizSetEntity("1", "teacher1", "Sample Quiz", null, 5, true, 0L))
        )
    }
}