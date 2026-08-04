package com.scholarwave.mobile.ui.results

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
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.data.local.AppDatabase
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
fun ResultsScreen(
    onBack: () -> Unit,
    onResultClick: (localId: Long, isMock: Boolean) -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val viewModel: ResultsViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        ResultsViewModel(
            resultDao = db.resultDao(),
            quizSetDao = db.quizSetDao(),
            mockExamResultDao = db.mockExamResultDao(),
            mockExamDao = db.mockExamDao(),
            authRepository = AuthRepository()
        )
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    Column(modifier.padding(16.dp)) {
        // ── Top Bar ───────────────────────────────────────────────────────────
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier.fillMaxWidth()
        ) {
            Button(onClick = onBack) {
                Text("← Back")
            }
            Spacer(Modifier.width(12.dp))
            Text("My Results", style = MaterialTheme.typography.headlineSmall)
        }

        Spacer(Modifier.height(16.dp))

        // ── Results List ──────────────────────────────────────────────────────
        when (val current = state) {
            ResultsUiState.Loading -> {
                Text("Loading results...", modifier = Modifier.padding(top = 16.dp))
            }
            is ResultsUiState.Success -> {
                if (current.results.isEmpty()) {
                    Text("No results yet — take a quiz to see it here.", modifier = Modifier.padding(top = 16.dp))
                } else {
                    val formatter = remember { SimpleDateFormat("MMM d, yyyy 'at' h:mm a", Locale.getDefault()) }
                    current.results.forEach { item ->
                        Card(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { onResultClick(item.localId, item.isMock) }
                                .padding(vertical = 4.dp)
                        ) {
                            Column(Modifier.padding(16.dp)) {
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        item.title,
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.Bold,
                                        modifier = Modifier.weight(1f)
                                    )
                                    // Kind tag
                                    val kindText = if (item.isMock) "Mock Exam" else "Practice Quiz"
                                    val kindColor = if (item.isMock)
                                        MaterialTheme.colorScheme.primary
                                    else
                                        MaterialTheme.colorScheme.secondary
                                    Text(
                                        kindText,
                                        style = MaterialTheme.typography.labelSmall,
                                        color = kindColor
                                    )
                                }
                                Spacer(Modifier.height(4.dp))
                                Text(
                                    "Score: ${item.score} / ${item.totalQuestions}",
                                    style = MaterialTheme.typography.bodyLarge
                                )
                                Spacer(Modifier.height(4.dp))
                                Row(
                                    modifier = Modifier.fillMaxWidth()
                                ) {
                                    Text(
                                        formatter.format(Date(item.submittedAtEpochMillis)),
                                        style = MaterialTheme.typography.bodySmall,
                                        modifier = Modifier.weight(1f)
                                    )
                                    Text(
                                        if (item.synced) "Synced" else "Pending sync",
                                        style = MaterialTheme.typography.bodySmall
                                    )
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}