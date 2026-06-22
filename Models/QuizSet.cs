using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuizApp.Models
{
    /// <summary>
    /// Represents a quiz set loaded from a JSON file
    /// </summary>
    public class QuizSet
    {
        /// <summary>
        /// Title of the quiz set
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the quiz set
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        /// <summary>
        /// Author of the quiz set (optional)
        /// </summary>
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        
        /// <summary>
        /// Difficulty level (optional)
        /// </summary>
        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }
        
        /// <summary>
        /// Time limit for the quiz in minutes
        /// </summary>
        [JsonPropertyName("timeLimitMinutes")]
        public int TimeLimitMinutes { get; set; } = Constants.DefaultTimeLimitMinutes;
        
        /// <summary>
        /// List of questions in the quiz set
        /// </summary>
        [JsonPropertyName("questions")]
        public List<Question> Questions { get; set; } = new List<Question>();
        
        /// <summary>
        /// File name of the quiz set (set at runtime, not from JSON)
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Validates the quiz set
        /// </summary>
        /// <returns>True if quiz set is valid</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Title))
                return false;
            
            if (Questions == null || Questions.Count == 0)
                return false;
            
            if (TimeLimitMinutes < Constants.MinTimeLimitMinutes || 
                TimeLimitMinutes > Constants.MaxTimeLimitMinutes)
                return false;
            
            // Validate each question
            foreach (var question in Questions)
            {
                if (string.IsNullOrWhiteSpace(question.Text))
                    return false;
                
                if (question.Options == null || question.Options.Length != 4)
                    return false;
                
                if (question.CorrectIndex < 0 || question.CorrectIndex >= 4)
                    return false;
                
                // Check for empty options
                foreach (var option in question.Options)
                {
                    if (string.IsNullOrWhiteSpace(option))
                        return false;
                }
            }
            
            return true;
        }
    }
}
