# Scholarwave CBT App

A simple Windows quiz application for teachers and students.

## What It Does

**For Teachers:**
- Create quizzes using a visual editor
- Load and manage quiz sets from JSON files
- Launch exams for students

**For Students:**  
- Take timed multiple-choice exams
- Navigate between questions
- Flag questions for review
- See results immediately

## Getting Started

### First Time Setup

1. **Set Password**: Edit `.env` file and change the password
   ```
   ADMIN_PASSWORD=your_password_here
   ```

2. **Run the app**:
   ```bash
   dotnet run
   ```

### Creating a Quiz

1. Login with your password
2. Click **"Create Quiz"**
3. Fill in quiz details (title, time limit)
4. Add questions with 4 options each
5. Save the quiz

### Starting an Exam

1. Login with your password
2. Select a quiz from the dropdown
3. Click **"LAUNCH EXAM MODE"**
4. Student enters their name and starts the exam

## Quiz Files

Quizzes are stored as JSON files in the `Questions/` folder. See `Questions/README.md` for the format.

## Results

Exam results are automatically saved to `results.csv` with timestamp, student name, class, and score.

---

**Need help?** Check `Questions/README.md` for quiz file format details.
