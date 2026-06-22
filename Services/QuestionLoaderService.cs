using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using QuizApp.Models;

namespace QuizApp.Services
{
    /// <summary>
    /// Service for loading quiz sets from JSON files in the Questions folder
    /// </summary>
    public static class QuestionLoaderService
    {
        /// <summary>
        /// Loads all valid quiz sets from the Questions folder
        /// </summary>
        /// <returns>List of valid quiz sets</returns>
        public static List<QuizSet> LoadAllQuizSets()
        {
            var quizSets = new List<QuizSet>();
            var invalidFiles = new List<string>();
            
            if (!Directory.Exists(Constants.QuestionsFolder))
            {
                Directory.CreateDirectory(Constants.QuestionsFolder);
                return quizSets;
            }
            
            var jsonFiles = Directory.GetFiles(Constants.QuestionsFolder, "*.json", SearchOption.AllDirectories);
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var quizSet = LoadQuizSet(file);
                    if (quizSet != null && quizSet.IsValid())
                    {
                        quizSets.Add(quizSet);
                    }
                    else
                    {
                        invalidFiles.Add($"{Path.GetFileName(file)} (Invalid format or missing fields)");
                    }
                }
                catch (Exception ex)
                {
                    invalidFiles.Add($"{Path.GetFileName(file)} (Error: {ex.Message})");
                }
            }
            
            // Notify user about invalid files if any
            if (invalidFiles.Count > 0)
            {
                string message = $"The following quiz files are invalid and were skipped:\n\n{string.Join("\n", invalidFiles)}";
                MessageBox.Show(message, "Invalid Quiz Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            return quizSets;
        }
        
        /// <summary>
        /// Loads a single quiz set from a JSON file
        /// </summary>
        /// <param name="filePath">Path to JSON file</param>
        /// <returns>QuizSet or null if invalid</returns>
        public static QuizSet? LoadQuizSet(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            
            try
            {
                string json = File.ReadAllText(filePath);
                var quizSet = JsonSerializer.Deserialize<QuizSet>(json);
                
                if (quizSet != null)
                {
                    quizSet.FileName = Path.GetFileName(filePath);
                }
                
                return quizSet;
            }
            catch
            {
                return null;
            }
        }
    }
}
