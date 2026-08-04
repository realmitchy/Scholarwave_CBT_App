package com.scholarwave.mobile

import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation3.runtime.entryProvider
import androidx.navigation3.runtime.rememberNavBackStack
import androidx.navigation3.ui.NavDisplay
import com.scholarwave.mobile.data.AuthRepository
import com.scholarwave.mobile.ui.auth.AuthScreen
import com.scholarwave.mobile.ui.exam.ExamScreen
import com.scholarwave.mobile.ui.main.MainScreen
import com.scholarwave.mobile.ui.mockexam.MockExamListScreen
import com.scholarwave.mobile.ui.mockexam.MockExamScreen
import com.scholarwave.mobile.ui.results.ResultsScreen
import com.scholarwave.mobile.ui.results.ResultDetailScreen

@Composable
fun MainNavigation() {
    val startDestination = if (AuthRepository().isLoggedIn) Main else Auth
    val backStack = rememberNavBackStack(startDestination)
    NavDisplay(
        backStack = backStack,
        onBack = { backStack.removeLastOrNull() },
        entryProvider =
            entryProvider {
                entry<Auth> {
                    AuthScreen(
                        onLoggedIn = {
                            backStack.clear()
                            backStack.add(Main)
                        },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
                entry<Main> {
                    MainScreen(
                        onItemClick = { navKey -> backStack.add(navKey) },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
                entry<Exam> { key ->
                    ExamScreen(
                        quizSetId = key.quizSetId,
                        onFinished = { backStack.removeLastOrNull() },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
                entry<Results> {
                    ResultsScreen(
                        onBack = { backStack.removeLastOrNull() },
                        onResultClick = { localId, isMock ->
                            backStack.add(ResultDetail(localId, isMock))
                        },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
                entry<ResultDetail> { key ->
                    ResultDetailScreen(
                        localId = key.localId,
                        isMock = key.isMock,
                        onBack = { backStack.removeLastOrNull() },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
                // ── Mock Exam entries ────────────────────────────────────────
                entry<MockExamList> {
                    MockExamListScreen(
                        onMockExamClick = { id -> backStack.add(MockExam(id)) },
                        onBack = { backStack.removeLastOrNull() },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
                entry<MockExam> { key ->
                    MockExamScreen(
                        mockExamId = key.mockExamId,
                        onFinished = { backStack.removeLastOrNull() },
                        modifier = Modifier.safeDrawingPadding().padding(16.dp)
                    )
                }
            },
    )
}