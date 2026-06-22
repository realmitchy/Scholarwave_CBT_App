using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Forms
{
    /// <summary>
    /// Teacher dashboard for managing quiz sets
    /// </summary>
    public class TeacherForm : Form
    {
        private ComboBox cmbQuizSet;
        private Button btnRefresh, btnCreateQuiz, btnStart;
        private Label lblQuestionCount, lblTotalTime;
        private List<QuizSet> availableQuizSets;
        
        public TeacherForm()
        {
            availableQuizSets = new List<QuizSet>();
            InitializeComponents();
            AttachEventHandlers();
            LoadQuizSets();
        }
        
        /// <summary>
        /// Initializes form controls
        /// </summary>
        private void InitializeComponents()
        {
            // Form properties
            Text = "Teacher Dashboard";
            Width = 520;
            Height = 400;
            StartPosition = FormStartPosition.CenterScreen;
            
            // Quiz set selector
            var lblQuizSet = new Label
            {
                Text = "Select Quiz Set:",
                Top = Constants.StandardSpacing,
                Left = Constants.StandardSpacing,
                Width = 120
            };
            
            cmbQuizSet = new ComboBox
            {
                Top = Constants.StandardSpacing,
                Left = 150,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // Refresh button
            btnRefresh = new Button
            {
                Text = "Refresh",
                Top = 60,
                Left = Constants.StandardSpacing,
                Width = Constants.ButtonWidth,
                Height = Constants.ButtonHeight
            };
            
            // Create Quiz button
            btnCreateQuiz = new Button
            {
                Text = "Create Quiz",
                Top = 60,
                Left = Constants.StandardSpacing + Constants.ButtonWidth + 10,
                Width = Constants.ButtonWidth,
                Height = Constants.ButtonHeight,
                BackColor = Color.LightSkyBlue
            };
            
            // Question count label
            lblQuestionCount = new Label
            {
                Text = "Questions: 0",
                Top = 110,
                Left = Constants.StandardSpacing,
                Width = 200,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
            };
            
            // Total time label
            lblTotalTime = new Label
            {
                Text = "Total Time: 0 minutes",
                Top = 140,
                Left = Constants.StandardSpacing,
                Width = 200,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
            };
            
            // Launch exam button
            btnStart = new Button
            {
                Text = "LAUNCH EXAM MODE",
                Top = 200,
                Left = Constants.StandardSpacing,
                Width = Constants.TextBoxWidth,
                Height = Constants.LargeButtonHeight,
                BackColor = Constants.LaunchButtonColor,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
            };
            
            // Add controls
            Controls.AddRange(new Control[] 
            {
                lblQuizSet, cmbQuizSet, btnRefresh, btnCreateQuiz,
                lblQuestionCount, lblTotalTime, btnStart
            });
        }
        
        /// <summary>
        /// Attaches event handlers - CRITICAL for button responsiveness
        /// </summary>
        private void AttachEventHandlers()
        {
            btnRefresh.Click += BtnRefresh_Click;
            btnCreateQuiz.Click += BtnCreateQuiz_Click;
            btnStart.Click += BtnStart_Click;
            cmbQuizSet.SelectedIndexChanged += CmbQuizSet_SelectedIndexChanged;
        }
        
        /// <summary>
        /// Loads all available quiz sets from Questions folder
        /// </summary>
        private void LoadQuizSets()
        {
            availableQuizSets = QuestionLoaderService.LoadAllQuizSets();
            
            cmbQuizSet.Items.Clear();
            
            if (availableQuizSets.Count == 0)
            {
                cmbQuizSet.Items.Add("No quiz sets found");
                cmbQuizSet.SelectedIndex = 0;
                cmbQuizSet.Enabled = false;
                btnStart.Enabled = false;
            }
            else
            {
                foreach (var quizSet in availableQuizSets)
                {
                    cmbQuizSet.Items.Add(quizSet.Title);
                }
                cmbQuizSet.SelectedIndex = 0;
                cmbQuizSet.Enabled = true;
                btnStart.Enabled = true;
            }
        }
        
        /// <summary>
        /// Refresh button click handler
        /// </summary>
        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadQuizSets();
        }
        
        /// <summary>
        /// Create Quiz button click handler
        /// </summary>
        private void BtnCreateQuiz_Click(object? sender, EventArgs e)
        {
            var editorForm = new QuizEditorForm();
            
            if (editorForm.ShowDialog() == DialogResult.OK)
            {
                // Refresh the quiz list after creating a new one
                LoadQuizSets();
            }
        }
        
        /// <summary>
        /// Quiz set selection changed handler
        /// </summary>
        private void CmbQuizSet_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbQuizSet.SelectedIndex >= 0 && cmbQuizSet.SelectedIndex < availableQuizSets.Count)
            {
                var selected = availableQuizSets[cmbQuizSet.SelectedIndex];
                lblQuestionCount.Text = $"Questions: {selected.Questions.Count}";
                lblTotalTime.Text = $"Total Time: {selected.TimeLimitMinutes} minutes";
            }
        }
        
        /// <summary>
        /// Launch exam button click handler
        /// </summary>
        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (cmbQuizSet.SelectedIndex < 0 || cmbQuizSet.SelectedIndex >= availableQuizSets.Count)
            {
                MessageBox.Show("Please select a quiz set.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var selectedQuizSet = availableQuizSets[cmbQuizSet.SelectedIndex];
            
            if (selectedQuizSet.Questions.Count == 0)
            {
                MessageBox.Show("The selected quiz set has no questions.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            this.Hide();
            var studentInfoForm = new StudentInfoForm(selectedQuizSet);
            studentInfoForm.FormClosed += (s2, e2) => this.Show();
            studentInfoForm.Show();
        }
    }
}
