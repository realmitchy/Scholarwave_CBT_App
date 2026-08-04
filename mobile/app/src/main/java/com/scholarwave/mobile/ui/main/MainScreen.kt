package com.scholarwave.mobile.ui.main

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation3.runtime.NavKey
import com.scholarwave.mobile.MockExam
import com.scholarwave.mobile.Results
import com.scholarwave.mobile.data.MockExamRepository
import com.scholarwave.mobile.data.local.AppDatabase
import com.scholarwave.mobile.data.local.MockExamEntity

@Composable
fun MainScreen(
    onItemClick: (NavKey) -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val viewModel: MainScreenViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        MainScreenViewModel(
            MockExamRepository(
                mockExamDao = db.mockExamDao(),
                mockExamSectionDao = db.mockExamSectionDao(),
                mockExamResultDao = db.mockExamResultDao(),
                mockExamSectionResultDao = db.mockExamSectionResultDao(),
                subjectDao = db.subjectDao(),
                quizSetDao = db.quizSetDao(),
                questionDao = db.questionDao()
            )
        )
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    LaunchedEffect(Unit) { viewModel.refresh() }

    Column(modifier.padding(16.dp)) {
        // ── Top Navigation ───────────────────────────────────────────────────
        Row {
            Button(onClick = { onItemClick(Results) }) {
                Text("My Results")
            }
        }

        Spacer(Modifier.height(16.dp))

        // ── Dashboard Content ────────────────────────────────────────────────
        Text("UTME Practice Exams", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(8.dp))

        when (val current = state) {
            MainScreenUiState.Loading -> {
                Text("Loading exams...", modifier = Modifier.padding(top = 16.dp))
            }
            is MainScreenUiState.Success -> {
                ExamDashboardContent(
                    exams = current.data,
                    onExamClick = { exam -> onItemClick(MockExam(exam.id)) }
                )
            }
            is MainScreenUiState.Error -> {
                Text(
                    "Error loading exams: ${current.throwable.message}",
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(top = 16.dp)
                )
            }
        }
    }
}

@Composable
private fun ExamDashboardContent(
    exams: List<MockExamEntity>,
    onExamClick: (MockExamEntity) -> Unit,
    modifier: Modifier = Modifier
) {
    if (exams.isEmpty()) {
        Text("No published exams yet.", modifier = modifier.padding(top = 8.dp))
        return
    }

    // Group mock exams by the 4-digit year found in their title. Fallback to "General Practice"
    val yearRegex = remember { Regex("\\b(20\\d{2})\\b") }
    val groupedExams = remember(exams) {
        exams.groupBy { exam ->
            yearRegex.find(exam.title)?.value ?: "General Practice"
        }.toSortedMap(compareByDescending { it }) // Sort years descending (e.g. 2026, 2025...)
    }

    Column(
        modifier = modifier
            .fillMaxWidth()
            .verticalScroll(rememberScrollState())
    ) {
        groupedExams.forEach { (year, examList) ->
            Text(
                text = if (year == "General Practice") year else "UTME Past Questions — $year",
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.secondary,
                modifier = Modifier.padding(vertical = 8.dp)
            )

            examList.forEach { exam ->
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 4.dp)
                        .clickable { onExamClick(exam) },
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant
                    )
                ) {
                    Column(Modifier.padding(16.dp)) {
                        Text(exam.title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                        Spacer(Modifier.height(4.dp))
                        Text(
                            "Duration: ${exam.durationMinutes} minutes  •  4 sections combined",
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                }
            }
            Spacer(Modifier.height(8.dp))
            HorizontalDivider()
        }
    }
}