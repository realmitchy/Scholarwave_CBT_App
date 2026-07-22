package com.scholarwave.mobile

import androidx.navigation3.runtime.NavKey
import kotlinx.serialization.Serializable

@Serializable data object Auth : NavKey
@Serializable data object Main : NavKey
@Serializable data class Exam(val quizSetId: String) : NavKey
@Serializable data object Results : NavKey