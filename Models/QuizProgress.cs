using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScholarwaveCBTApp.Models
{
    /// <summary>
    /// Represents the saved progress of an ongoing quiz session
    /// </summary>
    public class QuizProgress
    {
        [JsonPropertyName("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [JsonPropertyName("studentClass")]
        public string StudentClass { get; set; } = string.Empty;

        [JsonPropertyName("quizTitle")]
        public string QuizTitle { get; set; } = string.Empty;

        [JsonPropertyName("timeRemaining")]
        public int TimeRemaining { get; set; }

        [JsonPropertyName("currentIndex")]
        public int CurrentIndex { get; set; }

        [JsonPropertyName("questions")]
        public List<Question> Questions { get; set; } = new List<Question>();

        [JsonPropertyName("studentAnswers")]
        public int[] StudentAnswers { get; set; } = new int[0];

        [JsonPropertyName("flagged")]
        public bool[] Flagged { get; set; } = new bool[0];
    }
}
