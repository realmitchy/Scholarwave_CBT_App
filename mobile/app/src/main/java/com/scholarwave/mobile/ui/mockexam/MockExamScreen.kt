package com.scholarwave.mobile.ui.mockexam

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.selection.selectable
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.MockExamRepository
import com.scholarwave.mobile.data.local.AppDatabase

// ── Screen entry point ────────────────────────────────────────────────────────

@Composable
fun MockExamScreen(
    mockExamId: String,
    onFinished: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val viewModel: MockExamViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        MockExamViewModel(
            mockExamId = mockExamId,
            mockExamRepository = MockExamRepository(
                mockExamDao = db.mockExamDao(),
                mockExamSectionDao = db.mockExamSectionDao(),
                mockExamResultDao = db.mockExamResultDao(),
                mockExamSectionResultDao = db.mockExamSectionResultDao(),
                subjectDao = db.subjectDao(),
                quizSetDao = db.quizSetDao(),
                questionDao = db.questionDao()
            ),
            authRepository = AuthRepository()
        )
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    when (val current = state) {
        MockExamUiState.Loading -> {
            Text("Loading mock exam…", modifier = modifier.padding(16.dp))
        }

        is MockExamUiState.Error -> {
            Column(modifier.padding(16.dp)) {
                Text(current.message)
                Spacer(Modifier.height(16.dp))
                Button(onClick = onFinished) { Text("Back") }
            }
        }

        is MockExamUiState.InProgress -> {
            MockExamContent(
                state = current,
                viewModel = viewModel,
                onFinished = onFinished,
                modifier = modifier
            )
        }

        is MockExamUiState.Submitted -> {
            MockExamResultView(
                state = current,
                onFinished = onFinished,
                modifier = modifier
            )
        }
    }
}

// ── In-progress exam UI ───────────────────────────────────────────────────────

@Composable
private fun MockExamContent(
    state: MockExamUiState.InProgress,
    viewModel: MockExamViewModel,
    onFinished: () -> Unit,
    modifier: Modifier = Modifier
) {
    val section = state.sections[state.activeSectionIndex]
    val question = section.questions.getOrNull(state.currentQuestionIndex)
    val minutes = state.timeRemainingSeconds / 60
    val seconds = state.timeRemainingSeconds % 60
    val timerColor = if (state.timeRemainingSeconds < 300) Color.Red else Color.Unspecified

    Column(modifier.padding(16.dp)) {

        // ── Header: title + timer ─────────────────────────────────────────────
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                state.mockExamTitle,
                style = MaterialTheme.typography.titleSmall,
                modifier = Modifier.weight(1f)
            )
            Text(
                "%d:%02d".format(minutes, seconds),
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = timerColor
            )
        }

        Spacer(Modifier.height(8.dp))

        // ── Section tabs (pill/chip row) ──────────────────────────────────────
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            state.sections.forEachIndexed { index, sec ->
                FilterChip(
                    selected = index == state.activeSectionIndex,
                    onClick = { viewModel.switchSection(index) },
                    label = { Text(sec.subjectCode) }
                )
            }
        }

        // Answered count for current section
        Text(
            "${section.answeredCount}/${section.questions.size} answered in ${section.subjectName}",
            style = MaterialTheme.typography.bodySmall,
            modifier = Modifier.padding(top = 4.dp)
        )

        HorizontalDivider(Modifier.padding(vertical = 8.dp))

        if (question == null) {
            Text("No questions in this section.")
        } else {
            // ── Question ──────────────────────────────────────────────────────
            Text(
                "Q${state.currentQuestionIndex + 1} of ${section.questions.size}",
                style = MaterialTheme.typography.labelMedium
            )
            Spacer(Modifier.height(8.dp))
            Text(question.text, style = MaterialTheme.typography.bodyLarge)
            Spacer(Modifier.height(12.dp))

            // ── Options ───────────────────────────────────────────────────────
            val optionLabels = listOf("A", "B", "C", "D")
            question.options.forEachIndexed { index, option ->
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .selectable(
                            selected = section.answers[state.currentQuestionIndex] == index,
                            onClick = { viewModel.selectAnswer(index) }
                        )
                        .padding(vertical = 4.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    RadioButton(
                        selected = section.answers[state.currentQuestionIndex] == index,
                        onClick = { viewModel.selectAnswer(index) }
                    )
                    Spacer(Modifier.width(8.dp))
                    Text("${optionLabels.getOrElse(index) { "$index" }}. $option")
                }
            }

            Spacer(Modifier.height(8.dp))

            // ── Flag + nav ────────────────────────────────────────────────────
            TextButton(onClick = { viewModel.toggleFlag() }) {
                Text(
                    if (section.flagged[state.currentQuestionIndex])
                        "★ Flagged for review"
                    else
                        "☆ Flag for review"
                )
            }

            Spacer(Modifier.height(8.dp))
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                OutlinedButton(
                    onClick = { viewModel.previousQuestion() },
                    enabled = state.currentQuestionIndex > 0
                ) { Text("Previous") }

                if (state.currentQuestionIndex == section.questions.lastIndex) {
                    // Last question in section — show Next Section or Submit
                    val isLastSection = state.activeSectionIndex == state.sections.lastIndex
                    if (isLastSection) {
                        Button(onClick = { viewModel.submit() }) { Text("Submit Exam") }
                    } else {
                        Button(onClick = { viewModel.switchSection(state.activeSectionIndex + 1) }) {
                            Text("Next Section →")
                        }
                    }
                } else {
                    Button(onClick = { viewModel.nextQuestion() }) { Text("Next") }
                }
            }

            Spacer(Modifier.height(16.dp))

            // ── Early submit (always available) ───────────────────────────────
            OutlinedButton(
                onClick = { viewModel.submit() },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Submit Exam Now") }
        }
    }
}

// ── Results view (shown inline after submission) ──────────────────────────────

@Composable
private fun MockExamResultView(
    state: MockExamUiState.Submitted,
    onFinished: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(modifier.padding(16.dp)) {
        Text("Mock Exam Complete!", style = MaterialTheme.typography.headlineSmall)
        Spacer(Modifier.height(8.dp))
        Text(
            "Total: ${state.totalScore} / ${state.totalQuestions}",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )
        Spacer(Modifier.height(16.dp))
        Text("Subject Breakdown", style = MaterialTheme.typography.titleSmall)
        Spacer(Modifier.height(8.dp))

        state.sectionScores.forEach { section ->
            val pct = if (section.total > 0)
                (section.score * 100 / section.total)
            else 0

            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 4.dp),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceVariant
                )
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 10.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(section.subjectName, style = MaterialTheme.typography.bodyMedium)
                    Text(
                        "${section.score}/${section.total}  ($pct%)",
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.SemiBold
                    )
                }
            }
        }

        Spacer(Modifier.height(24.dp))
        Button(
            onClick = onFinished,
            modifier = Modifier.fillMaxWidth()
        ) { Text("Back to Main Menu") }
    }
}
