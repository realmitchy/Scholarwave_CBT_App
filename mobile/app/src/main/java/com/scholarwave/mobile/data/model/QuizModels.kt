package com.scholarwave.mobile.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

// ── Existing DTOs (extended with new nullable fields) ────────────────────────

@Serializable
data class QuizSetDto(
    val id: String? = null,
    @SerialName("teacher_id") val teacherId: String,
    val title: String,
    val description: String? = null,
    @SerialName("time_limit_minutes") val timeLimitMinutes: Int = 5,
    @SerialName("is_published") val isPublished: Boolean = true,
    @SerialName("subject_id") val subjectId: String? = null,     // NEW — schema migration
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
    @SerialName("subject_id") val subjectId: String? = null,     // NEW — schema migration
    @SerialName("exam_year") val examYear: Int? = null,          // NEW — schema migration
    val explanation: String? = null,                              // NEW — schema migration (nullable, empty for now)
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

// ── New DTOs — Mock Exam tables ───────────────────────────────────────────────

@Serializable
data class SubjectDto(
    val id: String,
    val name: String,
    val code: String                // e.g. "ENG", "PHY", "MATH"
)

@Serializable
data class MockExamDto(
    val id: String? = null,
    val title: String,
    @SerialName("created_by") val createdBy: String,
    @SerialName("duration_minutes") val durationMinutes: Int = 120,
    @SerialName("is_published") val isPublished: Boolean = false,
    @SerialName("created_at") val createdAt: String? = null
)

/**
 * One row in mock_exam_sections.
 * Links a mock exam to one quiz_set for a specific subject.
 * section_order: 0 = English (compulsory), 1–3 = electives.
 */
@Serializable
data class MockExamSectionDto(
    val id: String? = null,
    @SerialName("mock_exam_id") val mockExamId: String,
    @SerialName("quiz_set_id") val quizSetId: String,
    @SerialName("subject_id") val subjectId: String,
    @SerialName("section_order") val sectionOrder: Int
)

/**
 * One completed mock sitting — written after the student submits.
 * total_score and total_questions are raw counts across all sections.
 */
@Serializable
data class MockExamResultDto(
    val id: String? = null,
    @SerialName("mock_exam_id") val mockExamId: String,
    @SerialName("student_id") val studentId: String,
    @SerialName("total_score") val totalScore: Int,
    @SerialName("total_questions") val totalQuestions: Int,
    @SerialName("submitted_at") val submittedAt: String? = null,
    val synced: Boolean = false
)

/**
 * Per-subject sub-score within one mock sitting.
 * One row per section (so 4 rows per MockExamResult).
 */
@Serializable
data class MockExamSectionResultDto(
    val id: String? = null,
    @SerialName("mock_exam_result_id") val mockExamResultId: String,
    @SerialName("subject_id") val subjectId: String,
    val score: Int,
    @SerialName("total_questions") val totalQuestions: Int
)