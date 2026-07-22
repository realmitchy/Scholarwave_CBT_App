package com.scholarwave.mobile.ui.exam

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.selection.selectable
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.QuizRepository
import com.scholarwave.mobile.data.ResultsRepository
import com.scholarwave.mobile.data.local.AppDatabase

@Composable
fun ExamScreen(
    quizSetId: String,
    onFinished: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val viewModel: ExamViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        ExamViewModel(
            quizSetId = quizSetId,
            quizRepository = QuizRepository(db.quizSetDao(), db.questionDao()),
            resultsRepository = ResultsRepository(db.resultDao()),
            authRepository = AuthRepository(),
            quizProgressDao = db.quizProgressDao()
        )
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    when (val current = state) {
        ExamUiState.Loading -> Text("Loading exam...", modifier = modifier.padding(16.dp))

        is ExamUiState.Error -> {
            Column(modifier.padding(16.dp)) {
                Text(current.message)
                Spacer(Modifier.height(16.dp))
                Button(onClick = onFinished) { Text("Back to quizzes") }
            }
        }

        is ExamUiState.InProgress -> {
            ExamContent(state = current, viewModel = viewModel, modifier = modifier)
        }

        is ExamUiState.Submitted -> {
            Column(modifier.padding(16.dp)) {
                Text("Exam complete!")
                Spacer(Modifier.height(8.dp))
                Text("Score: ${current.score} / ${current.total}")
                Spacer(Modifier.height(16.dp))
                Button(onClick = { viewModel.retake() }) { Text("Retake this quiz") }
                Spacer(Modifier.height(8.dp))
                OutlinedButton(onClick = onFinished) { Text("Back to quizzes") }
            }
        }
    }
}

@Composable
private fun ExamContent(
    state: ExamUiState.InProgress,
    viewModel: ExamViewModel,
    modifier: Modifier = Modifier,
) {
    val question = state.questions[state.currentIndex]
    val minutes = state.timeRemainingSeconds / 60
    val seconds = state.timeRemainingSeconds % 60

    Column(modifier.padding(16.dp)) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text("Question ${state.currentIndex + 1} / ${state.questions.size}")
            Text("%d:%02d".format(minutes, seconds))
        }

        Spacer(Modifier.height(16.dp))
        Text(question.text)
        Spacer(Modifier.height(16.dp))

        question.options.forEachIndexed { index, option ->
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .selectable(
                        selected = state.answers[state.currentIndex] == index,
                        onClick = { viewModel.selectAnswer(index) }
                    )
                    .padding(vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                RadioButton(
                    selected = state.answers[state.currentIndex] == index,
                    onClick = { viewModel.selectAnswer(index) }
                )
                Spacer(Modifier.width(8.dp))
                Text(option)
            }
        }

        Spacer(Modifier.height(16.dp))
        TextButton(onClick = { viewModel.toggleFlag() }) {
            Text(if (state.flagged[state.currentIndex]) "Unflag this question" else "Flag for review")
        }

        Spacer(Modifier.height(16.dp))
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            OutlinedButton(
                onClick = { viewModel.previousQuestion() },
                enabled = state.currentIndex > 0
            ) { Text("Previous") }

            if (state.currentIndex == state.questions.lastIndex) {
                Button(onClick = { viewModel.submit() }) { Text("Submit") }
            } else {
                Button(onClick = { viewModel.nextQuestion() }) { Text("Next") }
            }
        }
    }
}