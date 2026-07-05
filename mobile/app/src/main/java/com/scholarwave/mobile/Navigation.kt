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
import com.scholarwave.mobile.ui.main.MainScreen

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
            },
    )
}