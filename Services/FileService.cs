using System;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using ScholarwaveCBTApp.Models;

namespace ScholarwaveCBTApp.Services
{
    /// <summary>
    /// Service for centralized file operations
    /// </summary>
    public static class FileService
    {
        /// <summary>
        /// Appends quiz result to CSV file with proper formatting
        /// </summary>
        /// <param name="studentName">Student name</param>
        /// <param name="studentClass">Student class</param>
        /// <param name="score">Score achieved</param>
        /// <param name="totalQuestions">Total questions</param>
        public static void SaveResult(string studentName, string studentClass, int score, int totalQuestions)
        {
            try
            {
                bool isNewFile = !File.Exists(Constants.ResultsFileName) || new FileInfo(Constants.ResultsFileName).Length == 0;
                
                using (StreamWriter sw = new StreamWriter(Constants.ResultsFileName, true))
                {
                    if (isNewFile)
                    {
                        sw.WriteLine("Timestamp,Student Name,Class,Score,Total Questions,Percentage");
                    }

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    string percentage = totalQuestions > 0 ? ((double)score / totalQuestions * 100).ToString("F2") : "0.00";
                    
                    // Escape fields that might contain commas
                    string safeName = EscapeCsv(studentName);
                    string safeClass = EscapeCsv(studentClass);
                    
                    sw.WriteLine($"{timestamp},{safeName},{safeClass},{score},{totalQuestions},{percentage}%");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save results: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        // --- Auto-Save Progress Methods ---
        
        private const string ProgressFileName = "current_progress.json";

        /// <summary>
        /// Saves the current quiz progress to a file
        /// </summary>
        public static void SaveProgress(QuizProgress progress)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });
                
                // Write to a temporary file first, then atomically move to the destination
                string tempFileName = ProgressFileName + ".tmp";
                File.WriteAllText(tempFileName, jsonString);
                File.Move(tempFileName, ProgressFileName, true);
            }
            catch (Exception ex)
            {
                // Silent catch for auto-save as we don't want to interrupt the user frequently
                System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if an incomplete quiz progress file exists
        /// </summary>
        public static bool HasProgress()
        {
            return File.Exists(ProgressFileName);
        }

        /// <summary>
        /// Loads the saved quiz progress
        /// </summary>
        public static QuizProgress? LoadProgress()
        {
            try
            {
                if (HasProgress())
                {
                    string jsonString = File.ReadAllText(ProgressFileName);
                    return JsonSerializer.Deserialize<QuizProgress>(jsonString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load previous session: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DeleteProgress(); // Clean up corrupted file so it doesn't persistently show the resume button
            }
            return null;
        }

        /// <summary>
        /// Deletes the progress file upon successful quiz submission
        /// </summary>
        public static void DeleteProgress()
        {
            try
            {
                if (HasProgress())
                {
                    File.Delete(ProgressFileName);
                }
                string tempFileName = ProgressFileName + ".tmp";
                if (File.Exists(tempFileName))
                {
                    File.Delete(tempFileName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete progress file: {ex.Message}");
            }
        }
    }
}
