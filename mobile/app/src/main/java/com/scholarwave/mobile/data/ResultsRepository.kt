package com.scholarwave.mobile.data

import com.scholarwave.mobile.data.local.ResultDao
import com.scholarwave.mobile.data.local.ResultEntity
import com.scholarwave.mobile.data.model.ResultDto
import com.scholarwave.mobile.data.remote.SupabaseHelper
import io.github.jan.supabase.postgrest.postgrest

/**
 * Handles exam result submission with a "never lose a result" guarantee:
 *
 *   1. submitResult() writes to Room FIRST — this must succeed even with
 *      zero connectivity, since a student could finish an exam anywhere.
 *   2. syncPendingResults() pushes anything not yet synced to Supabase,
 *      safe to call repeatedly (e.g. after submit, or periodically).
 */
class ResultsRepository(
    private val resultDao: ResultDao
) {
    private val postgrest get() = SupabaseHelper.client.postgrest

   suspend fun submitResult(
        quizSetId: String,
        studentId: String,
        studentName: String,
        studentClass: String,
        score: Int,
        totalQuestions: Int
    ) {
        resultDao.insert(
            ResultEntity(
                quizSetId = quizSetId,
                studentId = studentId,
                studentName = studentName,
                studentClass = studentClass,
                score = score,
                totalQuestions = totalQuestions,
                submittedAtEpochMillis = System.currentTimeMillis(),
                synced = false
            )
        )
        // Best-effort immediate sync; if this fails, the result is still
        // safely on-device and syncPendingResults() will retry later.
        runCatching { syncPendingResults() }
    }

    suspend fun syncPendingResults() {
        val pending = resultDao.getUnsynced()
        for (result in pending) {
            try {
                val inserted = postgrest["results"]
                    .insert(result.toDto()) { select() }
                    .decodeSingle<ResultDto>()

                val remoteId = inserted.id
                    ?: error("Supabase did not return an id for the inserted result")

                resultDao.markSynced(result.localId, remoteId)
            } catch (e: Exception) {
                resultDao.incrementSyncAttempts(result.localId)
                // Swallow and continue to the next pending result.
            }
        }
    }

    suspend fun getUnsyncedCount(): Int = resultDao.getUnsynced().size
}

private fun ResultEntity.toDto() = ResultDto(
    quizSetId = quizSetId,
    studentId = studentId,
    studentName = studentName,
    studentClass = studentClass,
    score = score,
    totalQuestions = totalQuestions
)