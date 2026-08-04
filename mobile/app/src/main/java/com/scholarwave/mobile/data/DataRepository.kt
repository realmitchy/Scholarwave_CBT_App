package com.scholarwave.mobile.data

import com.scholarwave.mobile.data.local.QuestionDao
import com.scholarwave.mobile.data.local.QuestionEntity
import com.scholarwave.mobile.data.local.QuizSetDao
import com.scholarwave.mobile.data.local.QuizSetEntity
import com.scholarwave.mobile.data.model.QuestionDto
import com.scholarwave.mobile.data.model.QuizSetDto
import com.scholarwave.mobile.data.remote.SupabaseHelper
import io.github.jan.supabase.postgrest.postgrest
import kotlinx.coroutines.flow.Flow

class QuizRepository(
    private val quizSetDao: QuizSetDao,
    private val questionDao: QuestionDao
) {
    private val postgrest get() = SupabaseHelper.client.postgrest

    fun observePublishedQuizzes(): Flow<List<QuizSetEntity>> = quizSetDao.observeAll()

    fun observeQuestions(quizSetId: String): Flow<List<QuestionEntity>> =
        questionDao.observeForQuiz(quizSetId)

    suspend fun getQuizSet(quizSetId: String): QuizSetEntity? = quizSetDao.getById(quizSetId)

    suspend fun refreshFromCloud() {
        val remoteQuizzes = postgrest["quiz_sets"]
            .select()
            .decodeList<QuizSetDto>()
            .filter { it.isPublished }
        quizSetDao.upsertAll(remoteQuizzes.map { it.toEntity() })
        for (quiz in remoteQuizzes) {
            val quizId = quiz.id ?: continue
            val remoteQuestions = postgrest["questions"]
                .select { filter { eq("quiz_set_id", quizId) } }
                .decodeList<QuestionDto>()
            questionDao.upsertAll(remoteQuestions.map { it.toEntity() })
        }
    }

    suspend fun publishQuiz(quizSet: QuizSetDto, questions: List<QuestionDto>) {
        val inserted = postgrest["quiz_sets"].insert(quizSet) { select() }.decodeSingle<QuizSetDto>()
        val quizId = inserted.id ?: error("Supabase did not return an id for the new quiz")
        val questionsWithParent = questions.map { it.copy(quizSetId = quizId) }
        postgrest["questions"].insert(questionsWithParent)
        quizSetDao.upsert(inserted.toEntity())
        questionDao.upsertAll(questionsWithParent.map { it.toEntity() })
    }
}

private fun QuizSetDto.toEntity() = QuizSetEntity(
    id = id ?: error("QuizSetDto missing id"),
    teacherId = teacherId,
    title = title,
    description = description,
    timeLimitMinutes = timeLimitMinutes,
    isPublished = isPublished,
    subjectId = subjectId,
    lastSyncedAt = System.currentTimeMillis()
)

private fun QuestionDto.toEntity() = QuestionEntity(
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