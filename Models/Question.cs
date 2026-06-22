using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScholarwaveCBTApp.Models
{
    /// <summary>
    /// Represents a single quiz question with multiple choice options
    /// </summary>
    public class Question
    {
        /// <summary>
        /// The question text
        /// </summary>
        [JsonPropertyName("text")]
        public required string Text { get; set; }
        
        /// <summary>
        /// Optional path to an image associated with the question
        /// </summary>
        [JsonPropertyName("imagePath")]
        public string? ImagePath { get; set; }
        
        /// <summary>
        /// Array of possible answer options
        /// </summary>
        [JsonPropertyName("options")]
        public required string[] Options { get; set; }
        
        /// <summary>
        /// Index of the correct answer in the Options array (0-based)
        /// </summary>
        [JsonPropertyName("correctIndex")]
        public int CorrectIndex { get; set; }
        
        /// <summary>
        /// Shuffled order of options for display (used at runtime and saved in progress)
        /// </summary>
        [JsonPropertyName("shuffledOrder")]
        public List<int>? ShuffledOrder { get; set; }
    }
}
