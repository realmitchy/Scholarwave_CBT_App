package com.scholarwave.mobile.data.local

import android.content.Context
import androidx.room.*
import kotlinx.coroutines.flow.Flow
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.decodeFromString

// ══════════════════════════════════════════════════════════════════════════════
// Entities
// ══════════════════════════════════════════════════════════════════════════════

@Entity(tableName = "quiz_sets")
data class QuizSetEntity(
    @PrimaryKey val id: String,
    val teacherId: String,
    val title: String,
    val description: String?,
    val timeLimitMinutes: Int,
    val isPublished: Boolean,
    val subjectId: String? = null,   // nullable — old rows have no subject
    val lastSyncedAt: Long
)

@Entity(tableName = "questions")
@TypeConverters(OptionsConverter::class)
data class QuestionEntity(
    @PrimaryKey val id: String,
    val quizSetId: String,
    val position: Int,
    val text: String,
    val options: List<String>,
    val correctIndex: Int,
    val imagePath: String?,
    val subjectId: String? = null,
    val examYear: Int? = null,
    val explanation: String? = null
)

@Entity(tableName = "results")
@TypeConverters(IntArrayConverter::class)
data class ResultEntity(
    @PrimaryKey(autoGenerate = true) val localId: Long = 0,
    val remoteId: String? = null,
    val quizSetId: String,
    val studentId: String,
    val studentName: String,
    val studentClass: String,
    val score: Int,
    val totalQuestions: Int,
    val studentAnswers: IntArray,   // NEW: Store student's chosen answer indexes
    val submittedAtEpochMillis: Long,
    val synced: Boolean = false,
    val syncAttempts: Int = 0
)

@Entity(tableName = "quiz_progress")
@TypeConverters(IntArrayConverter::class, BooleanArrayConverter::class)
data class QuizProgressEntity(
    @PrimaryKey val quizSetId: String,
    val studentName: String,
    val studentClass: String,
    val timeRemainingSeconds: Int,
    val currentIndex: Int,
    val studentAnswers: IntArray,
    val flagged: BooleanArray
)

// ── New entities — Mock Exam schema ──────────────────────────────────────────

@Entity(tableName = "subjects")
data class SubjectEntity(
    @PrimaryKey val id: String,
    val name: String,
    val code: String
)

@Entity(tableName = "mock_exams")
data class MockExamEntity(
    @PrimaryKey val id: String,
    val title: String,
    val createdBy: String,
    val durationMinutes: Int,
    val isPublished: Boolean,
    val lastSyncedAt: Long
)

/**
 * Cached section metadata. The actual questions are still fetched via
 * QuestionEntity (linked through quizSetId).
 */
@Entity(tableName = "mock_exam_sections")
data class MockExamSectionEntity(
    @PrimaryKey val id: String,
    val mockExamId: String,
    val quizSetId: String,
    val subjectId: String,
    val sectionOrder: Int
)

/**
 * One completed mock sitting attempt. Stored locally first, then synced.
 * localId is the Room primary key; remoteId is the Supabase-returned UUID.
 */
@Entity(tableName = "mock_exam_results")
data class MockExamResultEntity(
    @PrimaryKey(autoGenerate = true) val localId: Long = 0,
    val remoteId: String? = null,
    val mockExamId: String,
    val studentId: String,
    val totalScore: Int,
    val totalQuestions: Int,
    val submittedAtEpochMillis: Long,
    val synced: Boolean = false,
    val syncAttempts: Int = 0
)

/**
 * Per-subject sub-scores within one mock sitting.
 * mockExamResultLocalId links to MockExamResultEntity.localId.
 * mockExamResultRemoteId is populated after the parent row is synced.
 */
@Entity(tableName = "mock_exam_section_results")
@TypeConverters(IntArrayConverter::class)
data class MockExamSectionResultEntity(
    @PrimaryKey(autoGenerate = true) val localId: Long = 0,
    val mockExamResultLocalId: Long,
    val mockExamResultRemoteId: String? = null,
    val subjectId: String,
    val subjectName: String,   // denormalised for display without a join
    val score: Int,
    val totalQuestions: Int,
    val studentAnswers: IntArray,   // NEW: Store student's chosen answer indexes
    val synced: Boolean = false
)

// ══════════════════════════════════════════════════════════════════════════════
// Type converters
// ══════════════════════════════════════════════════════════════════════════════

class OptionsConverter {
    private val json = Json
    @TypeConverter fun fromList(value: List<String>): String = json.encodeToString(value)
    @TypeConverter fun toList(value: String): List<String> = json.decodeFromString(value)
}

class IntArrayConverter {
    private val json = Json
    @TypeConverter fun fromIntArray(value: IntArray): String = json.encodeToString(value.toList())
    @TypeConverter fun toIntArray(value: String): IntArray =
        json.decodeFromString<List<Int>>(value).toIntArray()
}

class BooleanArrayConverter {
    private val json = Json
    @TypeConverter fun fromBoolArray(value: BooleanArray): String =
        json.encodeToString(value.toList())
    @TypeConverter fun toBoolArray(value: String): BooleanArray =
        json.decodeFromString<List<Boolean>>(value).toBooleanArray()
}

// ══════════════════════════════════════════════════════════════════════════════
// DAOs
// ══════════════════════════════════════════════════════════════════════════════

@Dao
interface QuizSetDao {
    @Query("SELECT * FROM quiz_sets ORDER BY title ASC")
    fun observeAll(): Flow<List<QuizSetEntity>>

    @Query("SELECT * FROM quiz_sets WHERE id = :id")
    suspend fun getById(id: String): QuizSetEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(quizSets: List<QuizSetEntity>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(quizSet: QuizSetEntity)

    @Query("DELETE FROM quiz_sets WHERE id = :id")
    suspend fun delete(id: String)
}

@Dao
interface QuestionDao {
    @Query("SELECT * FROM questions WHERE quizSetId = :quizSetId ORDER BY position ASC")
    fun observeForQuiz(quizSetId: String): Flow<List<QuestionEntity>>

    @Query("SELECT * FROM questions WHERE quizSetId = :quizSetId ORDER BY position ASC")
    suspend fun getForQuiz(quizSetId: String): List<QuestionEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(questions: List<QuestionEntity>)

    @Query("DELETE FROM questions WHERE quizSetId = :quizSetId")
    suspend fun deleteForQuiz(quizSetId: String)
}

@Dao
interface ResultDao {
    @Insert
    suspend fun insert(result: ResultEntity): Long

    @Query("SELECT * FROM results WHERE localId = :localId")
    suspend fun getById(localId: Long): ResultEntity?

    @Query("SELECT * FROM results WHERE synced = 0")
    suspend fun getUnsynced(): List<ResultEntity>

    @Query("UPDATE results SET synced = 1, remoteId = :remoteId WHERE localId = :localId")
    suspend fun markSynced(localId: Long, remoteId: String)

    @Query("UPDATE results SET syncAttempts = syncAttempts + 1 WHERE localId = :localId")
    suspend fun incrementSyncAttempts(localId: Long)

    @Query("SELECT * FROM results WHERE quizSetId = :quizSetId ORDER BY submittedAtEpochMillis DESC")
    fun observeForQuiz(quizSetId: String): Flow<List<ResultEntity>>

    @Query("SELECT * FROM results WHERE studentId = :studentId ORDER BY submittedAtEpochMillis DESC")
    fun observeForStudent(studentId: String): Flow<List<ResultEntity>>
}

@Dao
interface QuizProgressDao {
    @Query("SELECT * FROM quiz_progress WHERE quizSetId = :quizSetId")
    suspend fun get(quizSetId: String): QuizProgressEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun save(progress: QuizProgressEntity)

    @Query("DELETE FROM quiz_progress WHERE quizSetId = :quizSetId")
    suspend fun delete(quizSetId: String)
}

// ── New DAOs — Mock Exam schema ───────────────────────────────────────────────

@Dao
interface SubjectDao {
    @Query("SELECT * FROM subjects ORDER BY name ASC")
    suspend fun getAll(): List<SubjectEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(subjects: List<SubjectEntity>)
}

@Dao
interface MockExamDao {
    @Query("SELECT * FROM mock_exams WHERE isPublished = 1 ORDER BY title ASC")
    fun observePublished(): Flow<List<MockExamEntity>>

    @Query("SELECT * FROM mock_exams WHERE id = :id")
    suspend fun getById(id: String): MockExamEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(mockExams: List<MockExamEntity>)
}

@Dao
interface MockExamSectionDao {
    @Query("SELECT * FROM mock_exam_sections WHERE mockExamId = :mockExamId ORDER BY sectionOrder ASC")
    suspend fun getSectionsForMockExam(mockExamId: String): List<MockExamSectionEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(sections: List<MockExamSectionEntity>)
}

@Dao
interface MockExamResultDao {
    @Insert
    suspend fun insert(result: MockExamResultEntity): Long

    @Query("SELECT * FROM mock_exam_results WHERE localId = :localId")
    suspend fun getById(localId: Long): MockExamResultEntity?

    @Query("SELECT * FROM mock_exam_results WHERE studentId = :studentId ORDER BY submittedAtEpochMillis DESC")
    fun observeForStudent(studentId: String): Flow<List<MockExamResultEntity>>

    @Query("SELECT * FROM mock_exam_results WHERE synced = 0")
    suspend fun getUnsynced(): List<MockExamResultEntity>

    @Query("UPDATE mock_exam_results SET synced = 1, remoteId = :remoteId WHERE localId = :localId")
    suspend fun markSynced(localId: Long, remoteId: String)

    @Query("UPDATE mock_exam_results SET syncAttempts = syncAttempts + 1 WHERE localId = :localId")
    suspend fun incrementSyncAttempts(localId: Long)
}

@Dao
interface MockExamSectionResultDao {
    @Insert
    suspend fun insertAll(sectionResults: List<MockExamSectionResultEntity>)

    @Query("SELECT * FROM mock_exam_section_results WHERE mockExamResultLocalId = :localId")
    suspend fun getForResult(localId: Long): List<MockExamSectionResultEntity>

    @Query("UPDATE mock_exam_section_results SET synced = 1, mockExamResultRemoteId = :remoteId WHERE mockExamResultLocalId = :localId")
    suspend fun markSyncedForResult(localId: Long, remoteId: String)
}

// ══════════════════════════════════════════════════════════════════════════════
// Database
// ══════════════════════════════════════════════════════════════════════════════

@Database(
    entities = [
        QuizSetEntity::class,
        QuestionEntity::class,
        ResultEntity::class,
        QuizProgressEntity::class,
        // New — mock exam schema
        SubjectEntity::class,
        MockExamEntity::class,
        MockExamSectionEntity::class,
        MockExamResultEntity::class,
        MockExamSectionResultEntity::class
    ],
    version = 3,                     // bumped to 3 for studentAnswers schema update
    exportSchema = false
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun quizSetDao(): QuizSetDao
    abstract fun questionDao(): QuestionDao
    abstract fun resultDao(): ResultDao
    abstract fun quizProgressDao(): QuizProgressDao
    // New DAOs
    abstract fun subjectDao(): SubjectDao
    abstract fun mockExamDao(): MockExamDao
    abstract fun mockExamSectionDao(): MockExamSectionDao
    abstract fun mockExamResultDao(): MockExamResultDao
    abstract fun mockExamSectionResultDao(): MockExamSectionResultDao

    companion object {
        @Volatile private var instance: AppDatabase? = null

        fun getInstance(context: Context): AppDatabase =
            instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(
                    context.applicationContext,
                    AppDatabase::class.java,
                    "scholarwave.db"
                )
                .fallbackToDestructiveMigration(dropAllTables = true)   // dev-only; replace with Migration() before production
                .build().also { instance = it }
            }
    }
}