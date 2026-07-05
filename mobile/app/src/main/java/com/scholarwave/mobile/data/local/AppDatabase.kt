package com.scholarwave.mobile.data.local

import android.content.Context
import androidx.room.*
import kotlinx.coroutines.flow.Flow
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.decodeFromString

// ---------- Entities ----------

@Entity(tableName = "quiz_sets")
data class QuizSetEntity(
    @PrimaryKey val id: String,
    val teacherId: String,
    val title: String,
    val description: String?,
    val timeLimitMinutes: Int,
    val isPublished: Boolean,
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
    val imagePath: String?
)

@Entity(tableName = "results")
data class ResultEntity(
    @PrimaryKey(autoGenerate = true) val localId: Long = 0,
    val remoteId: String? = null,
    val quizSetId: String,
    val studentId: String,
    val studentName: String,
    val studentClass: String,
    val score: Int,
    val totalQuestions: Int,
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

// ---------- Type converters ----------

class OptionsConverter {
    private val json = Json
    @TypeConverter
    fun fromList(value: List<String>): String = json.encodeToString(value)
    @TypeConverter
    fun toList(value: String): List<String> = json.decodeFromString(value)
}

class IntArrayConverter {
    private val json = Json
    @TypeConverter
    fun fromIntArray(value: IntArray): String = json.encodeToString(value.toList())
    @TypeConverter
    fun toIntArray(value: String): IntArray = json.decodeFromString<List<Int>>(value).toIntArray()
}

class BooleanArrayConverter {
    private val json = Json
    @TypeConverter
    fun fromBoolArray(value: BooleanArray): String = json.encodeToString(value.toList())
    @TypeConverter
    fun toBoolArray(value: String): BooleanArray = json.decodeFromString<List<Boolean>>(value).toBooleanArray()
}

// ---------- DAOs ----------

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

    @Query("SELECT * FROM results WHERE synced = 0")
    suspend fun getUnsynced(): List<ResultEntity>

    @Query("UPDATE results SET synced = 1, remoteId = :remoteId WHERE localId = :localId")
    suspend fun markSynced(localId: Long, remoteId: String)

    @Query("UPDATE results SET syncAttempts = syncAttempts + 1 WHERE localId = :localId")
    suspend fun incrementSyncAttempts(localId: Long)

    @Query("SELECT * FROM results WHERE quizSetId = :quizSetId ORDER BY submittedAtEpochMillis DESC")
    fun observeForQuiz(quizSetId: String): Flow<List<ResultEntity>>
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

// ---------- Database ----------

@Database(
    entities = [QuizSetEntity::class, QuestionEntity::class, ResultEntity::class, QuizProgressEntity::class],
    version = 1,
    exportSchema = false
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun quizSetDao(): QuizSetDao
    abstract fun questionDao(): QuestionDao
    abstract fun resultDao(): ResultDao
    abstract fun quizProgressDao(): QuizProgressDao

    companion object {
        @Volatile private var instance: AppDatabase? = null

        fun getInstance(context: Context): AppDatabase =
            instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(
                    context.applicationContext,
                    AppDatabase::class.java,
                    "scholarwave.db"
                ).build().also { instance = it }
            }
    }
}