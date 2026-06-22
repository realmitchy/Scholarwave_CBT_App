using System;
using System.Drawing;
using System.Windows.Forms;
using ScholarwaveCBTApp.Models;

namespace ScholarwaveCBTApp.Forms
{
    /// <summary>
    /// Student information registration form
    /// </summary>
    public class StudentInfoForm : Form
    {
        private TextBox txtName, txtClass;
        private Button btnStartExam, btnResumeSession;
        private Label lblQuizInfo;
        private QuizSet quizSet;
        
        public StudentInfoForm(QuizSet selectedQuizSet)
        {
            quizSet = selectedQuizSet;
            InitializeComponents();
            AttachEventHandlers();
        }
        
        /// <summary>
        /// Initializes form controls
        /// </summary>
        private void InitializeComponents()
        {
            // Form properties
            Text = "Student Registration";
            Width = 400;
            Height = 320;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(400, 320);
            
            // Quiz info label
            lblQuizInfo = new Label
            {
                Text = $"Quiz: {quizSet.Title}\nQuestions: {quizSet.Questions.Count}\nTime Limit: {quizSet.TimeLimitMinutes} minutes",
                Top = Constants.StandardSpacing,
                Left = Constants.StandardSpacing,
                Width = 350,
                Height = 60,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
            };
            
            // Name label and textbox
            var lblName = new Label
            {
                Text = "Full Name:",
                Top = 90,
                Left = Constants.StandardSpacing
            };
            
            txtName = new TextBox
            {
                Top = 110,
                Left = Constants.StandardSpacing,
                Width = 340
            };
            
            // Class label and textbox
            var lblClass = new Label
            {
                Text = "Class/Grade:",
                Top = 150,
                Left = Constants.StandardSpacing
            };
            
            txtClass = new TextBox
            {
                Top = 170,
                Left = Constants.StandardSpacing,
                Width = 340
            };
            
            // Start exam button
            btnStartExam = new Button
            {
                Text = "Start Exam",
                Top = 220,
                Left = Constants.StandardSpacing,
                Width = 340,
                Height = 40,
                BackColor = Constants.StartButtonColor,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
            };
            
            // Resume session button
            btnResumeSession = new Button
            {
                Text = "RESUME SAVED SESSION",
                Top = 265,
                Left = Constants.StandardSpacing,
                Width = 340,
                Height = 40,
                BackColor = Color.Orange,
                Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold),
                Visible = Services.FileService.HasProgress() // Only show if a save file exists
            };
            
            // Adjust form height if resume button is shown
            if (btnResumeSession.Visible)
            {
                Height = 365;
            }
            
            // Add controls
            Controls.AddRange(new Control[] 
            {
                lblQuizInfo, lblName, txtName, lblClass, txtClass, btnStartExam, btnResumeSession
            });
        }
        
        /// <summary>
        /// Attaches event handlers - CRITICAL for button responsiveness
        /// </summary>
        private void AttachEventHandlers()
        {
            btnStartExam.Click += BtnStartExam_Click;
            btnResumeSession.Click += BtnResumeSession_Click;
            txtName.KeyPress += TxtName_KeyPress;
            txtClass.KeyPress += TxtClass_KeyPress;
        }
        
        /// <summary>
        /// Start exam button click handler
        /// </summary>
        private void BtnStartExam_Click(object? sender, EventArgs e)
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter your name.", "Required Field", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            
            // Start the quiz
            this.Hide();
            var quizForm = new QuizForm(quizSet, txtName.Text, txtClass.Text);
            quizForm.FormClosed += (s2, e2) => this.Close();
            quizForm.Show();
        }
        
        /// <summary>
        /// Enter key support for name field
        /// </summary>
        private void TxtName_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)Keys.Return)
                return;
            
            txtClass.Focus();
        }
        
        /// <summary>
        /// Enter key support for class field
        /// </summary>
        private void TxtClass_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)Keys.Return)
                return;
            
            BtnStartExam_Click(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Resume session button click handler
        /// </summary>
        private void BtnResumeSession_Click(object? sender, EventArgs e)
        {
            var progress = Services.FileService.LoadProgress();
            
            if (progress != null)
            {
                // We show the quiz form and pass the progress object
                this.Hide();
                var quizForm = new QuizForm(quizSet, progress);
                quizForm.FormClosed += (s2, e2) => this.Close();
                quizForm.Show();
            }
            else
            {
                MessageBox.Show("Could not load the saved session. It might be corrupted or missing.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnResumeSession.Visible = false;
                Height = 320; // reset height
            }
        }
    }
}
