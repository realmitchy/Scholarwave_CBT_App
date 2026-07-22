package com.scholarwave.mobile.ui.results

import androidx.compose.runtime.remember
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
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
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val viewModel: ResultsViewModel = viewModel {
        val db = AppDatabase.getInstance(context)
        ResultsViewModel(db.resultDao(), db.quizSetDao(), AuthRepository())
    }
    val state by viewModel.uiState.collectAsStateWithLifecycle()

    Column(modifier.padding(16.dp)) {
        Text("My Results")

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
                        Column(Modifier.padding(vertical = 8.dp)) {
                            Text(item.quizTitle)
                            Text("Score: ${item.result.score} / ${item.result.totalQuestions}")
                            Text(formatter.format(Date(item.result.submittedAtEpochMillis)))
                            Text(if (item.result.synced) "Synced" else "Pending sync")
                        }
                        HorizontalDivider()
                    }
                }
            }
        }
    }
}