package com.scholarwave.mobile.data

import com.scholarwave.mobile.data.local.MockExamDao
import com.scholarwave.mobile.data.local.MockExamEntity
import com.scholarwave.mobile.data.local.MockExamResultDao
import com.scholarwave.mobile.data.local.MockExamResultEntity
import com.scholarwave.mobile.data.local.MockExamSectionDao
import com.scholarwave.mobile.data.local.MockExamSectionEntity
import com.scholarwave.mobile.data.local.MockExamSectionResultDao
import com.scholarwave.mobile.data.local.MockExamSectionResultEntity
import com.scholarwave.mobile.data.local.QuestionDao
import com.scholarwave.mobile.data.local.QuestionEntity
import com.scholarwave.mobile.data.local.QuizSetDao
import com.scholarwave.mobile.data.local.SubjectDao
import com.scholarwave.mobile.data.local.SubjectEntity
import com.scholarwave.mobile.data.model.MockExamDto
import com.scholarwave.mobile.data.model.MockExamResultDto
import com.scholarwave.mobile.data.model.MockExamSectionDto
import com.scholarwave.mobile.data.model.MockExamSectionResultDto
import com.scholarwave.mobile.data.model.QuestionDto
import com.scholarwave.mobile.data.model.QuizSetDto
import com.scholarwave.mobile.data.model.SubjectDto
import com.scholarwave.mobile.data.remote.SupabaseHelper
import io.github.jan.supabase.postgrest.postgrest
import kotlinx.coroutines.flow.Flow

// ── Domain model used by the ViewModel ───────────────────────────────────────

/**
 * All data needed to run one section of a mock exam in the UI.
 */
data class MockExamSection(
    val sectionEntity: MockExamSectionEntity,
    val subjectName: String,
    val subjectCode: String,
    val questions: List<QuestionEntity>
)

/**
 * Full mock exam ready for the exam screen.
 */
data class MockExamWithSections(
    val exam: MockExamEntity,
    val sections: List<MockExamSection>    // ordered by sectionOrder (English first)
)

// ── Repository ────────────────────────────────────────────────────────────────

class MockExamRepository(
    private val mockExamDao: MockExamDao,
    private val mockExamSectionDao: MockExamSectionDao,
    private val mockExamResultDao: MockExamResultDao,
    private val mockExamSectionResultDao: MockExamSectionResultDao,
    private val subjectDao: SubjectDao,
    private val quizSetDao: QuizSetDao,
    private val questionDao: QuestionDao
) {
    private val postgrest get() = SupabaseHelper.client.postgrest

    // ── Cloud sync ────────────────────────────────────────────────────────────

    /**
     * Fetches published mock exams + sections + subjects + their quiz sets /
     * questions from Supabase, and upserts everything into Room.
     * Safe to call on every screen entry.
     */
    suspend fun refreshFromCloud() {
        // 1. Subjects
        val remoteSubjects = postgrest["subjects"]
            .select()
            .decodeList<SubjectDto>()
        subjectDao.upsertAll(remoteSubjects.map { it.toEntity() })

        // 2. Published mock exams
        val remoteMockExams = postgrest["mock_exams"]
            .select { filter { eq("is_published", true) } }
            .decodeList<MockExamDto>()
        mockExamDao.upsertAll(remoteMockExams.map { it.toEntity() })

        // 3. Sections for those mock exams
        val mockExamIds = remoteMockExams.mapNotNull { it.id }
        val remoteSections = mutableListOf<MockExamSectionDto>()
        for (id in mockExamIds) {
            val sections = postgrest["mock_exam_sections"]
                .select { filter { eq("mock_exam_id", id) } }
                .decodeList<MockExamSectionDto>()
            remoteSections.addAll(sections)
        }
        mockExamSectionDao.upsertAll(remoteSections.map { it.toEntity() })

        // 4. Quiz sets referenced by those sections (may overlap with practice quiz sets)
        val quizSetIds = remoteSections.map { it.quizSetId }.distinct()
        for (quizSetId in quizSetIds) {
            val remoteQuizSets = postgrest["quiz_sets"]
                .select { filter { eq("id", quizSetId) } }
                .decodeList<QuizSetDto>()
            quizSetDao.upsertAll(remoteQuizSets.map { it.toEntity() })

            // 5. Questions for each quiz set
            val remoteQuestions = postgrest["questions"]
                .select { filter { eq("quiz_set_id", quizSetId) } }
                .decodeList<QuestionDto>()
            questionDao.upsertAll(remoteQuestions.map { it.toEntity() })
        }
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    /** Live list of published mock exams for the list screen. */
    fun observePublishedMockExams(): Flow<List<MockExamEntity>> =
        mockExamDao.observePublished()

    /**
     * Assembles a [MockExamWithSections] from Room — no network call.
     * Call refreshFromCloud() first to ensure the cache is warm.
     * Returns null if the mock exam is not found in Room.
     */
    suspend fun getMockExamWithSections(mockExamId: String): MockExamWithSections? {
        val exam = mockExamDao.getById(mockExamId) ?: return null
        val subjects = subjectDao.getAll().associateBy { it.id }
        val sectionEntities = mockExamSectionDao.getSectionsForMockExam(mockExamId)

        val sections = sectionEntities.map { section ->
            val subject = subjects[section.subjectId]
            MockExamSection(
                sectionEntity = section,
                subjectName = subject?.name ?: "Unknown",
                subjectCode = subject?.code ?: "?",
                questions = questionDao.getForQuiz(section.quizSetId)
            )
        }
        return MockExamWithSections(exam = exam, sections = sections)
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    /**
     * Persists a completed mock sitting to Room first (guaranteed),
     * then best-effort syncs to Supabase.
     *
     * @param sectionScores List of (subjectId, subjectName, score, totalQuestions)
     *                      in the same order as the sections.
     */
    suspend fun submitMockResult(
        mockExamId: String,
        studentId: String,
        totalScore: Int,
        totalQuestions: Int,
        sectionScores: List<SectionScore>
    ): Long {
        // Write parent row
        val localId = mockExamResultDao.insert(
            MockExamResultEntity(
                mockExamId = mockExamId,
                studentId = studentId,
                totalScore = totalScore,
                totalQuestions = totalQuestions,
                submittedAtEpochMillis = System.currentTimeMillis(),
                synced = false
            )
        )

        // Write per-section rows
        mockExamSectionResultDao.insertAll(
            sectionScores.map { s ->
                MockExamSectionResultEntity(
                    mockExamResultLocalId = localId,
                    subjectId = s.subjectId,
                    subjectName = s.subjectName,
                    score = s.score,
                    totalQuestions = s.totalQuestions,
                    studentAnswers = s.studentAnswers
                )
            }
        )

        // Best-effort immediate sync; if this fails the data is safe in Room
        runCatching { syncPendingResults() }

        return localId
    }

    /**
     * Reconstructs a past mock result attempt, loading sections, questions, and selected answers.
     */
    suspend fun getMockResultDetail(localResultId: Long): PastMockResultDetail? {
        val result = mockExamResultDao.getById(localResultId) ?: return null
        val exam = mockExamDao.getById(result.mockExamId)
        val title = exam?.title ?: "Unknown Mock Exam"

        val sections = mockExamSectionResultDao.getForResult(localResultId)
        val mockExamSections = mockExamSectionDao.getSectionsForMockExam(result.mockExamId).associateBy { it.subjectId }

        val sectionDetails = sections.map { sec ->
            val quizSetId = mockExamSections[sec.subjectId]?.quizSetId
            val questions = if (quizSetId != null) {
                questionDao.getForQuiz(quizSetId)
            } else emptyList()

            PastMockSectionResult(
                subjectName = sec.subjectName,
                score = sec.score,
                totalQuestions = sec.totalQuestions,
                studentAnswers = sec.studentAnswers.toList(),
                questions = questions
            )
        }
        return PastMockResultDetail(result, title, sectionDetails)
    }

    /**
     * Pushes all unsynced mock results (and their section sub-scores) to
     * Supabase. Safe to call repeatedly.
     */
    suspend fun syncPendingResults() {
        val pending = mockExamResultDao.getUnsynced()
        for (result in pending) {
            try {
                // Insert parent row
                val inserted = postgrest["mock_exam_results"]
                    .insert(result.toDto()) { select() }
                    .decodeSingle<MockExamResultDto>()

                val remoteId = inserted.id
                    ?: error("Supabase did not return an id for mock_exam_result")

                mockExamResultDao.markSynced(result.localId, remoteId)

                // Insert section sub-scores, now that we have the remote parent id
                val sectionResults = mockExamSectionResultDao.getForResult(result.localId)
                postgrest["mock_exam_section_results"].insert(
                    sectionResults.map { it.toDto(remoteId) }
                )
                mockExamSectionResultDao.markSyncedForResult(result.localId, remoteId)

            } catch (e: Exception) {
                mockExamResultDao.incrementSyncAttempts(result.localId)
                // Swallow and continue to the next pending result
            }
        }
    }
}

// ── Domain model for section scores (used by ViewModel + submitMockResult) ───

data class SectionScore(
    val subjectId: String,
    val subjectName: String,
    val score: Int,
    val totalQuestions: Int,
    val studentAnswers: IntArray
)

// ── Domain model for past result reviews ─────────────────────────────────────

data class PastMockResultDetail(
    val result: MockExamResultEntity,
    val mockExamTitle: String,
    val sections: List<PastMockSectionResult>
)

data class PastMockSectionResult(
    val subjectName: String,
    val score: Int,
    val totalQuestions: Int,
    val studentAnswers: List<Int>,
    val questions: List<QuestionEntity>
)

// ── Mapping helpers ───────────────────────────────────────────────────────────

private fun SubjectDto.toEntity() = SubjectEntity(id = id, name = name, code = code)

private fun MockExamDto.toEntity() = MockExamEntity(
    id = id ?: error("MockExamDto missing id"),
    title = title,
    createdBy = createdBy,
    durationMinutes = durationMinutes,
    isPublished = isPublished,
    lastSyncedAt = System.currentTimeMillis()
)

private fun MockExamSectionDto.toEntity() = MockExamSectionEntity(
    id = id ?: error("MockExamSectionDto missing id"),
    mockExamId = mockExamId,
    quizSetId = quizSetId,
    subjectId = subjectId,
    sectionOrder = sectionOrder
)

private fun QuizSetDto.toEntity() = com.scholarwave.mobile.data.local.QuizSetEntity(
    id = id ?: error("QuizSetDto missing id"),
    teacherId = teacherId,
    title = title,
    description = description,
    timeLimitMinutes = timeLimitMinutes,
    isPublished = isPublished,
    subjectId = subjectId,
    lastSyncedAt = System.currentTimeMillis()
)

private fun QuestionDto.toEntity() = com.scholarwave.mobile.data.local.QuestionEntity(
    id = id ?: error("QuestionDto missing id"),
    quizSetId = quizSetId,
    position = position,
    text = text,
    options = options,
    correctIndex = correctIndex,
    imagePath = imagePath,
    subjectId = subjectId,
    examYear = examYear,
    explanation = explanation
)

private fun MockExamResultEntity.toDto() = MockExamResultDto(
    mockExamId = mockExamId,
    studentId = studentId,
    totalScore = totalScore,
    totalQuestions = totalQuestions
)

private fun MockExamSectionResultEntity.toDto(remoteParentId: String) =
    MockExamSectionResultDto(
        mockExamResultId = remoteParentId,
        subjectId = subjectId,
        score = score,
        totalQuestions = totalQuestions
    )
