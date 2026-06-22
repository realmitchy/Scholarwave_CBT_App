using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using QuizApp.Models;
using QuizApp.UI;

namespace QuizApp.Forms
{
    /// <summary>
    /// Form for creating and editing quiz sets
    /// </summary>
    public class QuizEditorForm : MaterialAppForm
    {
        private TextBox txtTitle, txtDescription, txtAuthor, txtDifficulty;
        private NumericUpDown numTimeLimit;
        private ListBox lstQuestions;
        private TextBox txtQuestion, txtOptionA, txtOptionB, txtOptionC, txtOptionD;
        private ComboBox cmbCorrectAnswer;
        private Button btnAddQuestion, btnEditQuestion, btnDeleteQuestion, btnSaveQuiz, btnCancel;
        private QuizSet currentQuiz;
        private int editingIndex = -1;
        
        public QuizEditorForm()
        {
            currentQuiz = new QuizSet();
            InitializeComponents();
            AttachEventHandlers();
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        /// <summary>
        /// Initializes form controls
        /// </summary>
        private void InitializeComponents()
        {
            // Form properties
            Text = "Quiz Editor";
            Width = 700;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(700, 700);
            
            int leftCol = 20;
            int rightCol = 120;
            int row = 20;
            
            // Quiz metadata section
            Controls.Add(new Label { Text = "Quiz Title:", Left = leftCol, Top = row, Width = 90 });
            txtTitle = new TextBox { Left = rightCol, Top = row, Width = 540 };
            Controls.Add(txtTitle);
            row += 35;
            
            Controls.Add(new Label { Text = "Description:", Left = leftCol, Top = row, Width = 90 });
            txtDescription = new TextBox { Left = rightCol, Top = row, Width = 540 };
            Controls.Add(txtDescription);
            row += 35;
            
            Controls.Add(new Label { Text = "Author:", Left = leftCol, Top = row, Width = 90 });
            txtAuthor = new TextBox { Left = rightCol, Top = row, Width = 250 };
            Controls.Add(txtAuthor);
            
            Controls.Add(new Label { Text = "Difficulty:", Left = 390, Top = row, Width = 70 });
            txtDifficulty = new TextBox { Left = 470, Top = row, Width = 190 };
            Controls.Add(txtDifficulty);
            row += 35;
            
            Controls.Add(new Label { Text = "Time (mins):", Left = leftCol, Top = row, Width = 90 });
            numTimeLimit = new NumericUpDown 
            { 
                Left = rightCol, 
                Top = row, 
                Width = 100,
                Minimum = Constants.MinTimeLimitMinutes,
                Maximum = Constants.MaxTimeLimitMinutes,
                Value = Constants.DefaultTimeLimitMinutes
            };
            Controls.Add(numTimeLimit);
            row += 45;
            
            // Questions section
            var lblQuestions = new Label 
            { 
                Text = "Questions:", 
                Left = leftCol, 
                Top = row, 
                Width = 200,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
            };
            Controls.Add(lblQuestions);
            row += 25;
            
            lstQuestions = new ListBox 
            { 
                Left = leftCol, 
                Top = row, 
                Width = 640, 
                Height = 100 
            };
            Controls.Add(lstQuestions);
            row += 110;
            
            // Question editor section
            Controls.Add(new Label { Text = "Question Text:", Left = leftCol, Top = row, Width = 100 });
            row += 20;
            txtQuestion = new TextBox { Left = leftCol, Top = row, Width = 640, Height = 40, Multiline = true };
            Controls.Add(txtQuestion);
            row += 50;
            
            Controls.Add(new Label { Text = "Option A:", Left = leftCol, Top = row, Width = 70 });
            txtOptionA = new TextBox { Left = 100, Top = row, Width = 560 };
            Controls.Add(txtOptionA);
            row += 35;
            
            Controls.Add(new Label { Text = "Option B:", Left = leftCol, Top = row, Width = 70 });
            txtOptionB = new TextBox { Left = 100, Top = row, Width = 560 };
            Controls.Add(txtOptionB);
            row += 35;
            
            Controls.Add(new Label { Text = "Option C:", Left = leftCol, Top = row, Width = 70 });
            txtOptionC = new TextBox { Left = 100, Top = row, Width = 560 };
            Controls.Add(txtOptionC);
            row += 35;
            
            Controls.Add(new Label { Text = "Option D:", Left = leftCol, Top = row, Width = 70 });
            txtOptionD = new TextBox { Left = 100, Top = row, Width = 560 };
            Controls.Add(txtOptionD);
            row += 35;
            
            Controls.Add(new Label { Text = "Correct Answer:", Left = leftCol, Top = row, Width = 120 });
            cmbCorrectAnswer = new ComboBox 
            { 
                Left = 150, 
                Top = row, 
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCorrectAnswer.Items.AddRange(new[] { "Option A", "Option B", "Option C", "Option D" });
            cmbCorrectAnswer.SelectedIndex = 0;
            Controls.Add(cmbCorrectAnswer);
            row += 45;
            
            // Question management buttons
            btnAddQuestion = new Button 
            { 
                Text = "Add Question", 
                Left = leftCol, 
                Top = row, 
                Width = 140,
                Height = Constants.ButtonHeight
            };
            Controls.Add(btnAddQuestion);
            
            btnEditQuestion = new Button 
            { 
                Text = "Update Question", 
                Left = 180, 
                Top = row, 
                Width = 140,
                Height = Constants.ButtonHeight,
                Enabled = false
            };
            Controls.Add(btnEditQuestion);
            
            btnDeleteQuestion = new Button 
            { 
                Text = "Delete Question", 
                Left = 340, 
                Top = row, 
                Width = 140,
                Height = Constants.ButtonHeight
            };
            Controls.Add(btnDeleteQuestion);
            row += 50;
            
            // Save/Cancel buttons
            btnSaveQuiz = new Button 
            { 
                Text = "Save Quiz to File", 
                Left = leftCol, 
                Top = row, 
                Width = 200,
                Height = 40
            };
            Controls.Add(btnSaveQuiz);
            
            btnCancel = new Button 
            { 
                Text = "Cancel", 
                Left = 460, 
                Top = row, 
                Width = 200,
                Height = 40
            };
            Controls.Add(btnCancel);

            ThemeManager.ApplyToForm(this);
            ThemeManager.StyleSubmitButton(btnSaveQuiz);
        }
        
        /// <summary>
        /// Attaches event handlers
        /// </summary>
        private void AttachEventHandlers()
        {
            btnAddQuestion.Click += BtnAddQuestion_Click;
            btnEditQuestion.Click += BtnEditQuestion_Click;
            btnDeleteQuestion.Click += BtnDeleteQuestion_Click;
            btnSaveQuiz.Click += BtnSaveQuiz_Click;
            btnCancel.Click += BtnCancel_Click;
            lstQuestions.SelectedIndexChanged += LstQuestions_SelectedIndexChanged;
        }
        
        /// <summary>
        /// Add question button click handler
        /// </summary>
        private void BtnAddQuestion_Click(object? sender, EventArgs e)
        {
            if (!ValidateQuestionInputs())
                return;
            
            var question = new Question
            {
                Text = txtQuestion.Text.Trim(),
                Options = new[] 
                { 
                    txtOptionA.Text.Trim(), 
                    txtOptionB.Text.Trim(), 
                    txtOptionC.Text.Trim(), 
                    txtOptionD.Text.Trim() 
                },
                CorrectIndex = cmbCorrectAnswer.SelectedIndex
            };
            
            currentQuiz.Questions.Add(question);
            RefreshQuestionsList();
            ClearQuestionInputs();
        }
        
        /// <summary>
        /// Edit question button click handler
        /// </summary>
        private void BtnEditQuestion_Click(object? sender, EventArgs e)
        {
            if (editingIndex < 0 || currentQuiz.Questions == null || editingIndex >= currentQuiz.Questions.Count)
                return;
            
            if (!ValidateQuestionInputs())
                return;
            
            currentQuiz.Questions[editingIndex] = new Question
            {
                Text = txtQuestion.Text.Trim(),
                Options = new[] 
                { 
                    txtOptionA.Text.Trim(), 
                    txtOptionB.Text.Trim(), 
                    txtOptionC.Text.Trim(), 
                    txtOptionD.Text.Trim() 
                },
                CorrectIndex = cmbCorrectAnswer.SelectedIndex
            };
            
            RefreshQuestionsList();
            ClearQuestionInputs();
            editingIndex = -1;
            btnEditQuestion.Enabled = false;
            btnAddQuestion.Enabled = true;
        }
        
        /// <summary>
        /// Delete question button click handler
        /// </summary>
        private void BtnDeleteQuestion_Click(object? sender, EventArgs e)
        {
            if (lstQuestions.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a question to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var result = MessageBox.Show("Delete this question?", "Confirm Delete", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes)
                return;
            
            currentQuiz.Questions.RemoveAt(lstQuestions.SelectedIndex);
            RefreshQuestionsList();
            ClearQuestionInputs();
        }
        
        /// <summary>
        /// Save quiz button click handler
        /// </summary>
        private void BtnSaveQuiz_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Please enter a quiz title.", "Required Field", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }
            
            if (currentQuiz.Questions.Count == 0)
            {
                MessageBox.Show("Please add at least one question.", "No Questions", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Set quiz metadata
            currentQuiz.Title = txtTitle.Text.Trim();
            currentQuiz.Description = txtDescription.Text.Trim();
            currentQuiz.Author = txtAuthor.Text.Trim();
            currentQuiz.Difficulty = txtDifficulty.Text.Trim();
            currentQuiz.TimeLimitMinutes = (int)numTimeLimit.Value;
            
            // Validate
            if (!currentQuiz.IsValid())
            {
                MessageBox.Show("Quiz validation failed. Please check all fields.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Save file dialog
            using var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), Constants.QuestionsFolder),
                FileName = SanitizeFileName(currentQuiz.Title) + ".json"
            };
            
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            
            try
            {
                string json = JsonSerializer.Serialize(currentQuiz, _jsonOptions);
                File.WriteAllText(dialog.FileName, json);
                
                MessageBox.Show($"Quiz saved successfully to:\n{Path.GetFileName(dialog.FileName)}", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save quiz: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Cancel button click handler
        /// </summary>
        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        /// <summary>
        /// Questions list selection changed handler
        /// </summary>
        private void LstQuestions_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstQuestions.SelectedIndex < 0)
                return;
            
            editingIndex = lstQuestions.SelectedIndex;
            var question = currentQuiz.Questions[editingIndex];
            
            txtQuestion.Text = question.Text;
            txtOptionA.Text = question.Options[0];
            txtOptionB.Text = question.Options[1];
            txtOptionC.Text = question.Options[2];
            txtOptionD.Text = question.Options[3];
            cmbCorrectAnswer.SelectedIndex = question.CorrectIndex;
            
            btnEditQuestion.Enabled = true;
            btnAddQuestion.Enabled = false;
        }
        
        /// <summary>
        /// Validates question inputs
        /// </summary>
        private bool ValidateQuestionInputs()
        {
            if (string.IsNullOrWhiteSpace(txtQuestion.Text))
            {
                MessageBox.Show("Please enter a question.", "Required Field", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQuestion.Focus();
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(txtOptionA.Text) || 
                string.IsNullOrWhiteSpace(txtOptionB.Text) ||
                string.IsNullOrWhiteSpace(txtOptionC.Text) || 
                string.IsNullOrWhiteSpace(txtOptionD.Text))
            {
                MessageBox.Show("All four options must be filled in.", "Required Fields", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Refreshes the questions list display
        /// </summary>
        private void RefreshQuestionsList()
        {
            lstQuestions.Items.Clear();
            for (int i = 0; i < currentQuiz.Questions.Count; i++)
            {
                lstQuestions.Items.Add($"Q{i + 1}: {currentQuiz.Questions[i].Text}");
            }
        }
        
        /// <summary>
        /// Clears question input fields
        /// </summary>
        private void ClearQuestionInputs()
        {
            txtQuestion.Clear();
            txtOptionA.Clear();
            txtOptionB.Clear();
            txtOptionC.Clear();
            txtOptionD.Clear();
            cmbCorrectAnswer.SelectedIndex = 0;
            editingIndex = -1;
            btnEditQuestion.Enabled = false;
            btnAddQuestion.Enabled = true;
        }
        
        /// <summary>
        /// Sanitizes filename by removing invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
