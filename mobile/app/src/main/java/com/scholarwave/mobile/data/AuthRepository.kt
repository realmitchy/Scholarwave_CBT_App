package com.scholarwave.mobile.data

import com.scholarwave.mobile.data.remote.SupabaseHelper
import io.github.jan.supabase.auth.auth
import io.github.jan.supabase.auth.providers.builtin.Email
import io.github.jan.supabase.postgrest.postgrest
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class ProfileDto(
    val id: String,
    val role: String,
    @SerialName("full_name") val fullName: String,
    @SerialName("student_class") val studentClass: String? = null
)

class AuthRepository {
    private val auth get() = SupabaseHelper.client.auth
    private val postgrest get() = SupabaseHelper.client.postgrest

    val currentUserId: String? get() = auth.currentUserOrNull()?.id

    /**
     * Returns true if the account is immediately usable (logged in now),
     * or false if Supabase is requiring email confirmation first — this
     * lets the UI show "check your email" without any future code
     * changes when that setting gets turned on later.
     */
    suspend fun signUpStudent(email: String, password: String, fullName: String, studentClass: String): Boolean {
        auth.signUpWith(Email) {
            this.email = email
            this.password = password
        }
        val userId = currentUserId

        if (userId == null) {
            // Email confirmation is required — no active session yet.
            // The profile row gets created on first successful sign-in instead.
            // The database trigger will handle the initial profile creation when confirmed.
            return false
        }

        try {
            postgrest["profiles"].insert(
                ProfileDto(id = userId, role = "student", fullName = fullName, studentClass = studentClass)
            )
        } catch (e: Exception) {
            // Ignore if profile already exists (e.g. created by database trigger)
        }
        return true
    }

    /**
     * Called after a successful sign-in if no profile exists yet — covers
     * the case where sign-up happened before email confirmation was
     * required, or the profile insert didn't happen at sign-up time.
     */
    suspend fun ensureProfileExists(fullName: String, studentClass: String) {
        val userId = currentUserId ?: return
        val existing = getMyProfile()
        if (existing == null) {
            try {
                postgrest["profiles"].insert(
                    ProfileDto(id = userId, role = "student", fullName = fullName, studentClass = studentClass)
                )
            } catch (e: Exception) {
                // Ignore if profile already exists or failed to insert
            }
        }
    }

    suspend fun signIn(email: String, password: String) {
        auth.signInWith(Email) {
            this.email = email
            this.password = password
        }
    }

    suspend fun getMyProfile(): ProfileDto? {
        val userId = currentUserId ?: return null
        return postgrest["profiles"]
            .select { filter { eq("id", userId) } }
            .decodeSingleOrNull<ProfileDto>()
    }

    fun signOut() {
        // Fire-and-forget is fine here; caller's screen should navigate
        // away regardless of network state.
    }

    val isLoggedIn: Boolean get() = currentUserId != null
}