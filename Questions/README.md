# Quiz JSON Format Documentation

This folder contains quiz sets in JSON format. Each file represents a complete quiz that can be loaded by the application.

## File Format

Each quiz JSON file should follow this structure:

```json
{
  "title": "Quiz Title",
  "description": "Brief description of the quiz",
  "author": "Author name (optional)",
  "difficulty": "Easy/Medium/Hard (optional)",
  "timeLimitMinutes": 10,
  "questions": [
    {
      "text": "Question text here?",
      "options": ["Option A", "Option B", "Option C", "Option D"],
      "correctIndex": 0
    }
  ]
}
```

## Field Descriptions

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| **title** | ✅ Yes | string | Name of the quiz set |
| **description** | ❌ No | string | Short description of what the quiz covers |
| **author** | ❌ No | string | Creator of the quiz |
| **difficulty** | ❌ No | string | Difficulty level indicator |
| **timeLimitMinutes** | ✅ Yes | number | Time limit in minutes for the entire quiz |
| **questions** | ✅ Yes | array | List of question objects |
| **questions[].text** | ✅ Yes | string | The question text |
| **questions[].options** | ✅ Yes | array | Array of exactly 4 answer options |
| **questions[].correctIndex** | ✅ Yes | number | Index (0-3) of the correct answer in the options array |

## Validation Rules

1. Every quiz must have a **title** and at least **one question**
2. Time limit must be between 1 and 300 minutes
3. Each question must have exactly **4 options**
4. `correctIndex` must be 0, 1, 2, or 3
5. All text fields cannot be empty

## Creating Your Own Quizzes

1. Create a new `.json` file in this folder
2. Follow the format above
3. Ensure all required fields are present
4. Test by loading it in the Teacher Dashboard

Invalid quiz files will be reported when loading and automatically skipped.
