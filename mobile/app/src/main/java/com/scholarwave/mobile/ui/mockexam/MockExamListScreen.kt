package com.scholarwave.mobile.ui.mockexam

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import androidx.lifecycle.viewmodel.compose.viewModel
import com.scholarwave.mobile.data.MockExamRepository
import com.scholarwave.mobile.data.local.AppDatabase
import com.scholarwave.mobile.data.local.MockExamEntity
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

// ── ViewModel ─────────────────────────────────────────────────────────────────

sealed interface MockExamListUiState {
    object Loading : MockExamListUiState
    data class Error(val message: String) : MockExamListUiState
    data class Success(val mockExams: List<MockExamEntity>) : MockExamListUiState
}

class MockExamListViewModel(
    private val repository: MockExamRepository
) : ViewModel() {

    val uiState: StateFlow<MockExamListUiState> =
        repository.observePublishedMockExams()
            .map<List<MockExamEntity>, MockExamListUiState>(MockExamListUiState::Success)
            .catch { emit(MockExamListUiState.Error(it.message ?: "Unknown error")) }
            .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), MockExamListUiState.Loading)

    fun refresh() {
        viewModelScope.launch {
            runCatching { repository.refreshFromCloud() }
        }
    }
}

// ── Screen ────────────────────────────────────────────────────────────────────

@Composable
fun MockExamListScreen(
    onMockExamClick: (mockExamId: String) -> Unit,
    onBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val viewModel: MockExamListViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        MockExamListViewModel(
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
    LaunchedEffect(Unit) { viewModel.refresh() }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    Column(modifier.padding(16.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Button(onClick = onBack) { Text("← Back") }
            Spacer(Modifier.width(12.dp))
            Text("Mock Exams", style = MaterialTheme.typography.headlineSmall)
        }

        Spacer(Modifier.height(16.dp))

        when (val current = state) {
            MockExamListUiState.Loading -> {
                Text("Loading mock exams…", modifier = Modifier.padding(top = 16.dp))
            }

            is MockExamListUiState.Error -> {
                Text(
                    "Couldn't refresh — ${current.message}",
                    modifier = Modifier.padding(top = 16.dp)
                )
            }

            is MockExamListUiState.Success -> {
                if (current.mockExams.isEmpty()) {
                    Text(
                        "No published mock exams yet.",
                        modifier = Modifier.padding(top = 16.dp)
                    )
                } else {
                    current.mockExams.forEach { exam ->
                        MockExamCard(
                            exam = exam,
                            onClick = { onMockExamClick(exam.id) }
                        )
                        Spacer(Modifier.height(8.dp))
                    }
                }
            }
        }
    }
}

@Composable
private fun MockExamCard(
    exam: MockExamEntity,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(exam.title, style = MaterialTheme.typography.titleMedium)
            Spacer(Modifier.height(4.dp))
            Text(
                "Duration: ${exam.durationMinutes} minutes  •  4 sections",
                style = MaterialTheme.typography.bodySmall
            )
        }
    }
}
