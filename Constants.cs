using System.Drawing;

namespace ScholarwaveCBTApp
{
    /// <summary>
    /// Application-wide constants for configuration, UI, and validation
    /// </summary>
    public static class Constants
    {
        // File Paths
        public const string ResultsFileName = "results.csv";
        public const string ErrorLogFileName = "error.log";
        public const string QuestionsFolder = "Questions";
        public const string ImagesFolder = "Images";
        
        // Default Values
        public const int DefaultTimeLimitMinutes = 5;
        public const int MinTimeLimitMinutes = 1;
        public const int MaxTimeLimitMinutes = 300;
        
        // UI Dimensions
        public const int ButtonWidth = 140;
        public const int ButtonHeight = 35;
        public const int LargeButtonHeight = 60;
        public const int StandardSpacing = 20;
        public const int TextBoxWidth = 460;
        
        // UI Colors
        public static readonly Color LoginButtonColor = Color.AliceBlue;
        public static readonly Color LaunchButtonColor = Color.PaleGreen;
        public static readonly Color StartButtonColor = Color.LightBlue;
        public static readonly Color SubmitButtonColor = Color.Crimson;
        public static readonly Color AnsweredQuestionColor = Color.LightGreen;
        public static readonly Color FlaggedQuestionColor = Color.Yellow;
        public static readonly Color UnansweredQuestionColor = Color.White;
        public static readonly Color ActiveQuestionBorderColor = Color.Blue;
        public static readonly Color TimerTextColor = Color.Red;
        public static readonly Color NavPanelBackColor = Color.FromArgb(240, 240, 240);
        
        // Fonts
        public const string DefaultFontFamily = "Segoe UI";
        public const float DefaultFontSize = 9F;
        public const float LargeFontSize = 12F;
        public const float SmallFontSize = 10F;
    }
}
