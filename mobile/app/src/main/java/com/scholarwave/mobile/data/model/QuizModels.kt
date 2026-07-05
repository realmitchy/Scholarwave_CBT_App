package com.scholarwave.mobile.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class QuizSetDto(
    val id: String? = null,
    @SerialName("teacher_id") val teacherId: String,
    val title: String,
    val description: String? = null,
    @SerialName("time_limit_minutes") val timeLimitMinutes: Int = 5,
    @SerialName("is_published") val isPublished: Boolean = true,
    @SerialName("created_at") val createdAt: String? = null,
    @SerialName("updated_at") val updatedAt: String? = null
)

@Serializable
data class QuestionDto(
    val id: String? = null,
    @SerialName("quiz_set_id") val quizSetId: String,
    val position: Int = 0,
    val text: String,
    val options: List<String>,
    @SerialName("correct_index") val correctIndex: Int,
    @SerialName("image_path") val imagePath: String? = null,
    @SerialName("created_at") val createdAt: String? = null
)

@Serializable
data class ResultDto(
    val id: String? = null,
    @SerialName("quiz_set_id") val quizSetId: String,
    @SerialName("student_id") val studentId: String,
    @SerialName("student_name") val studentName: String,
    @SerialName("student_class") val studentClass: String,
    val score: Int,
    @SerialName("total_questions") val totalQuestions: Int,
    @SerialName("submitted_at") val submittedAt: String? = null
)