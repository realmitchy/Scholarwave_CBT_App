using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QuizApp.Models;
using QuizApp.Services;
using QuizApp.UI;

namespace QuizApp.Forms
{
    /// <summary>
    /// Quiz form for taking exams
    /// </summary>
    public class QuizForm : MaterialAppForm
    {
        private string studentName;
        private string studentClass;
        private Label lblQuestion, lblTimer, lblTotalTime;
        private ProgressBar prgTime;
        private RadioButton[] options;
        private Button btnNext, btnPrev, btnSubmit;
        private CheckBox chkFlag;
        private PictureBox picQuestionImage;
        private FlowLayoutPanel navPanel;
        private System.Windows.Forms.Timer? quizTimer;
        private int currentIndex = 0;
        private int timeRemaining;
        private List<Question> questions;
        private int[] studentAnswers;
        private bool[] flagged;
        private Button[] navButtons;
        private QuizSet quizSet;
        private DateTime quizStartTime;
		private Button btnCalculator;
		private CalculatorForm? calculatorForm;
        
        public QuizForm(QuizSet selectedQuizSet, string name, string classGrade)
        {
            studentName = name;
            studentClass = classGrade ?? string.Empty;
            quizSet = selectedQuizSet;
            
            // Shuffle questions
            questions = quizSet.Questions.OrderBy(x => Guid.NewGuid()).ToList();
            studentAnswers = Enumerable.Repeat(-1, questions.Count).ToArray();
            flagged = new bool[questions.Count];
            timeRemaining = quizSet.TimeLimitMinutes * 60;
            
            // Shuffle options for each question
            foreach (var q in questions)
            {
                q.ShuffledOrder = Enumerable.Range(0, 4).OrderBy(x => Guid.NewGuid()).ToList();
            }
            
            InitializeComponents();
            AttachEventHandlers();
            StartQuiz();
        }

        /// <summary>
        /// Constructor for resuming a saved session
        /// </summary>
        public QuizForm(QuizSet selectedQuizSet, QuizProgress savedProgress)
        {
            quizSet = selectedQuizSet;
            
            // Restore from progress
            studentName = savedProgress.StudentName;
            studentClass = savedProgress.StudentClass;
            questions = savedProgress.Questions;
            timeRemaining = savedProgress.TimeRemaining;
            currentIndex = savedProgress.CurrentIndex;
            studentAnswers = savedProgress.StudentAnswers;
            flagged = savedProgress.Flagged;
            
            InitializeComponents();
            AttachEventHandlers();
            UpdateTimerDisplay(); // Ensure timer label is updated with resumed time immediately
            StartQuiz();
        }

        /// <summary>
        /// Updates the visual timer label
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (prgTime != null && timeRemaining <= prgTime.Maximum && timeRemaining >= 0)
            {
                prgTime.Value = timeRemaining;
            }
            
            var timeSpan = TimeSpan.FromSeconds(timeRemaining);
            
            if (lblTotalTime != null)
            {
                lblTotalTime.Text = $"Time Remaining: {timeSpan.Minutes:D2} minutes, {timeSpan.Seconds:D2} seconds";
            }
            
            if (lblTimer != null)
            {
                lblTimer.Text = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
        }
        
        /// <summary>
        /// Initializes form controls
        /// </summary>
        private void InitializeComponents()
        {
            // Form properties
            Text = $"Exam in Progress: {studentName}";
            Width = 900;
            Height = 580;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(900, 580);
            ThemeManager.ApplyFormSurface(this);
            
            // Total time label
            lblTotalTime = new Label
            {
                Text = "Time Remaining: ...",
                Top = 30,
                Left = Constants.StandardSpacing,
                Width = 610
            };
            ThemeManager.StyleAccentLabel(lblTotalTime, italic: true);
            
            // Progress bar
            prgTime = new ProgressBar
            {
                Top = 10,
                Left = Constants.StandardSpacing,
                Width = 610,
                Height = 15,
                Maximum = quizSet.TimeLimitMinutes * 60, // Use total defined time
                Value = timeRemaining <= quizSet.TimeLimitMinutes * 60 ? timeRemaining : quizSet.TimeLimitMinutes * 60
            };
            
            // Timer label
            lblTimer = new Label
            {
                Top = 10,
                Left = 560,
                Width = 70,
                Font = new Font(Constants.DefaultFontFamily, Constants.SmallFontSize, FontStyle.Bold)
            };
            ThemeManager.StyleAccentLabel(lblTimer);
            
            // Question label
            lblQuestion = new Label
            {
                Top = 55,
                Left = Constants.StandardSpacing,
                Width = 600,
                Height = 90,
                Font = new Font(Constants.DefaultFontFamily, Constants.LargeFontSize, FontStyle.Bold)
            };
            ThemeManager.StyleLabel(lblQuestion, isTitle: true);
            
            // Picture Box for image
            picQuestionImage = new PictureBox
            {
                Top = 150,
                Left = Constants.StandardSpacing,
                Width = 600,
                Height = 200, // Fixed height for images
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false // Hidden by default
            };
            
            // Radio button options
            options = new RadioButton[4];
            for (int i = 0; i < 4; i++)
            {
                options[i] = new RadioButton
                {
                    Top = 160 + i * 50,
                    Left = 40,
                    Width = 580,
                    Font = new Font(Constants.DefaultFontFamily, 11F)
                };
                ThemeManager.StyleRadioButton(options[i]);
                Controls.Add(options[i]);
            }
            
            // Flag checkbox
            chkFlag = new CheckBox
            {
                Text = "Flag for Review",
                Top = 370,
                Left = 40,
                Width = 150
            };
            ThemeManager.StyleCheckBox(chkFlag);
            
            // Navigation buttons
            btnPrev = new Button
            {
                Text = "Back",
                Top = 420,
                Left = 40,
                Width = 110,
                Height = 45
            };
            ThemeManager.StyleStandardButton(btnPrev);
            IconTheme.ApplyToButton(btnPrev, AppIcons.ArrowBack);
            
            btnNext = new Button
            {
                Text = "Next",
                Top = 420,
                Left = 160,
                Width = 110,
                Height = 45
            };
            ThemeManager.StyleStandardButton(btnNext);
            IconTheme.ApplyToButton(btnNext, AppIcons.ArrowForward);
            
            btnSubmit = new Button
            {
                Text = "Submit",
                Top = 420,
                Left = 460,
                Width = 150,
                Height = 45
            };
            ThemeManager.StyleSubmitButton(btnSubmit);
            IconTheme.ApplyToButton(btnSubmit, AppIcons.Send, 18);

            // Navigation panel
            navPanel = new FlowLayoutPanel
            {
                Top = 10,
                Left = 650,
                Width = 220,
                Height = 450,
                AutoScroll = true,
                BackColor = Constants.NavPanelBackColor
            };
            
			// Calculator toggle button (bottom-right)
			btnCalculator = new Button
			{
				Text = "Calculator",
				AutoSize = false,
				Width = 110,
				Height = 36,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			ThemeManager.StyleStandardButton(btnCalculator);
			IconTheme.ApplyToButton(btnCalculator, AppIcons.Calculate);
			// Position will be finalized after size is known in ShowQuestion()
			btnCalculator.Click += (s, e) => ToggleCalculator();
			
            // Create navigation buttons
            navButtons = new Button[questions.Count];
            for (int i = 0; i < questions.Count; i++)
            {
                int index = i; // Capture for lambda
                navButtons[i] = new Button
                {
                    Text = (i + 1).ToString(),
                    Width = 45,
                    Height = 45
                };
                ThemeManager.StyleNavButton(navButtons[i], Constants.UnansweredQuestionColor);
                // Event handler will be attached in AttachEventHandlers
                navPanel.Controls.Add(navButtons[i]);
            }
            
            // Add controls
            Controls.AddRange(new Control[]
            {
                prgTime, lblTotalTime, lblTimer, lblQuestion, picQuestionImage,
				btnPrev, btnNext, btnSubmit, navPanel, chkFlag, btnCalculator
            });

			btnCalculator.BringToFront();

			// Reposition calc form when moving/resizing
			Move += (s, e) => RepositionCalculator();
			ResizeEnd += (s, e) => RepositionCalculator();
			Resize += (s, e) => RepositionCalculator();
        }
        
        /// <summary>
        /// Attaches event handlers - CRITICAL for button responsiveness
        /// </summary>
        private void AttachEventHandlers()
        {
            btnPrev.Click += BtnPrev_Click;
            btnNext.Click += BtnNext_Click;
            btnSubmit.Click += BtnSubmit_Click;
            chkFlag.CheckedChanged += ChkFlag_CheckedChanged;
            FormClosing += QuizForm_FormClosing;
            
            // Attach navigation button handlers
            for (int i = 0; i < navButtons.Length; i++)
            {
                int index = i; // Capture for lambda
                navButtons[i].Click += (s, e) =>
                {
                    SaveState();
                    currentIndex = index;
                    ShowQuestion();
                };
            }
        }
        
        /// <summary>
        /// Starts the quiz timer and shows first question
        /// </summary>
        private void StartQuiz()
        {
            quizStartTime = DateTime.Now;
            UpdateTimerDisplay(); // Ensure timer starts showing immediately
            quizTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            quizTimer.Tick += QuizTimer_Tick;
            quizTimer.Start();
            ShowQuestion();
        }
        
        /// <summary>
        /// Timer tick event handler
        /// </summary>
        private void QuizTimer_Tick(object? sender, EventArgs e)
        {
            timeRemaining--;
            
            if (timeRemaining < 0)
            {
                FinishQuiz();
                return;
            }
            
            UpdateTimerDisplay();

            // Auto-save every 30 seconds
            if (timeRemaining % 30 == 0)
            {
                FileService.SaveProgress(GetProgressState());
            }
        }
        
        /// <summary>
        /// Shows the current question
        /// </summary>
        private void ShowQuestion()
        {
            var q = questions[currentIndex];
            lblQuestion.Text = $"Question {currentIndex + 1} of {questions.Count}:\n{q.Text}";
            
            bool hasImage = !string.IsNullOrEmpty(q.ImagePath);
            
            if (hasImage)
            {
                try
                {
                    // ImagePath could be absolute or relative. Assuming relative to base dir or Images folder
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    string imageFullPath = System.IO.Path.Combine(currentDir, Constants.ImagesFolder, q.ImagePath);
                    
                    if (!System.IO.File.Exists(imageFullPath))
                    {
                        // Fallback check: just relative to the exe
                        imageFullPath = System.IO.Path.Combine(currentDir, q.ImagePath);
                    }
                    
                    if (System.IO.File.Exists(imageFullPath))
                    {
                        // Dispose previous image if any
                        picQuestionImage.Image?.Dispose();
                        
                        // Use FileStream to prevent file lock
                        using (var stream = new System.IO.FileStream(imageFullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            picQuestionImage.Image = System.Drawing.Image.FromStream(stream);
                        }
                        picQuestionImage.Visible = true;
                    }
                    else
                    {
                        picQuestionImage.Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
                    picQuestionImage.Visible = false;
                }
            }
            else
            {
                picQuestionImage.Visible = false;
                picQuestionImage.Image?.Dispose();
                picQuestionImage.Image = null;
            }
            
            // Adjust options position based on image visibility
            int optionsStartY = picQuestionImage.Visible ? picQuestionImage.Bottom + 10 : 160;
            
            if (q.ShuffledOrder != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    options[i].Top = optionsStartY + (i * 45); // slightly smaller gap
                    options[i].Text = q.Options[q.ShuffledOrder[i]];
                    options[i].Checked = (studentAnswers[currentIndex] == i);
                }
            }
            
            // Adjust bottom control positions dynamically below the last option
            int lastOptionBottom = options[3].Bottom;
            chkFlag.Top = lastOptionBottom + 15;
            btnPrev.Top = chkFlag.Bottom + 15;
            btnNext.Top = btnPrev.Top;
            btnSubmit.Top = btnPrev.Top;

			// Place calculator button near bottom-right inside client area
			btnCalculator.Left = this.ClientSize.Width - btnCalculator.Width - 16;
			btnCalculator.Top = this.ClientSize.Height - btnCalculator.Height - 14;

            // Expand form size dynamically if needed to fit the bottom buttons
            int requiredHeight = btnSubmit.Bottom + 60; // 60 for padding/window border
            if (requiredHeight > 580)
            {
                this.Height = requiredHeight;
            }
            else if (this.Height < 580)
            {
                this.Height = 580; // Keep a usable minimum but do not shrink a user-resized window
            }

            // Adjust navPanel height to match form height
            navPanel.Height = this.ClientSize.Height - 40;

            chkFlag.Checked = flagged[currentIndex];
            UpdateNavColor();
        }

		private void ToggleCalculator()
		{
			if (calculatorForm == null || calculatorForm.IsDisposed)
			{
				calculatorForm = new CalculatorForm();
				calculatorForm.Show(this);
				RepositionCalculator();
			}
			else if (calculatorForm.Visible)
			{
				calculatorForm.Hide();
			}
			else
			{
				calculatorForm.Show(this);
				RepositionCalculator();
			}
		}

		private void RepositionCalculator()
		{
			if (calculatorForm == null || calculatorForm.IsDisposed || !calculatorForm.Visible) return;

			// Position calculator at the bottom-right corner of this form (just outside or overlapping slightly)
			// Convert client bottom-right to screen coordinates
			var clientBottomRight = new Point(this.ClientSize.Width, this.ClientSize.Height);
			var screenPoint = this.PointToScreen(clientBottomRight);

			int marginX = 12;
			int marginY = 12;

			int calcX = screenPoint.X - calculatorForm.Width - marginX;
			int calcY = screenPoint.Y - calculatorForm.Height - marginY;

			calculatorForm.Location = new Point(Math.Max(0, calcX), Math.Max(0, calcY));
		}
        
        /// <summary>
        /// Saves the current answer state to arrays
        /// </summary>
        private void SaveState()
        {
            studentAnswers[currentIndex] = -1;
            for (int i = 0; i < 4; i++)
            {
                if (options[i].Checked)
                {
                    studentAnswers[currentIndex] = i;
                }
            }
            UpdateNavColor();
        }

        /// <summary>
        /// Packages the current progress into a QuizProgress object
        /// </summary>
        private QuizProgress GetProgressState()
        {
            SaveState(); // ensure current UI options are recorded
            return new QuizProgress
            {
                StudentName = studentName,
                StudentClass = studentClass,
                QuizTitle = quizSet.Title,
                TimeRemaining = timeRemaining,
                CurrentIndex = currentIndex,
                Questions = questions,
                StudentAnswers = studentAnswers,
                Flagged = flagged
            };
        }
        
        /// <summary>
        /// Updates navigation button colors based on answer status
        /// </summary>
        private void UpdateNavColor()
        {
            for (int i = 0; i < navButtons.Length; i++)
            {
                // Set background color
                if (flagged[i])
                    ThemeManager.StyleNavButton(navButtons[i], Constants.FlaggedQuestionColor);
                else if (studentAnswers[i] != -1)
                    ThemeManager.StyleNavButton(navButtons[i], Constants.AnsweredQuestionColor);
                else
                    ThemeManager.StyleNavButton(navButtons[i], Constants.UnansweredQuestionColor);
                
                // Highlight current question
                if (i == currentIndex)
                {
                    navButtons[i].FlatStyle = FlatStyle.Flat;
                    navButtons[i].FlatAppearance.BorderSize = 3;
                    navButtons[i].FlatAppearance.BorderColor = Constants.ActiveQuestionBorderColor;
                }
                else
                {
                    navButtons[i].FlatStyle = FlatStyle.Standard;
                    navButtons[i].FlatAppearance.BorderSize = 1;
                }
            }
        }
        
        /// <summary>
        /// Previous button click handler
        /// </summary>
        private void BtnPrev_Click(object? sender, EventArgs e)
        {
            SaveState();
            if (currentIndex > 0)
            {
                currentIndex--;
                ShowQuestion();
            }
        }
        
        /// <summary>
        /// Next button click handler
        /// </summary>
        private void BtnNext_Click(object? sender, EventArgs e)
        {
            SaveState();
            if (currentIndex < questions.Count - 1)
            {
                currentIndex++;
                ShowQuestion();
            }
        }
        
        /// <summary>
        /// Submit button click handler
        /// </summary>
        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Finalize and Submit?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                FinishQuiz();
            }
        }
        
        /// <summary>
        /// Flag checkbox changed handler
        /// </summary>
        private void ChkFlag_CheckedChanged(object? sender, EventArgs e)
        {
            flagged[currentIndex] = chkFlag.Checked;
            UpdateNavColor();
        }
        
        /// <summary>
        /// Form closing event handler with confirmation
        /// </summary>
        private void QuizForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (quizTimer != null && quizTimer.Enabled)
            {
                var result = MessageBox.Show("Are you sure you want to stop the exam? You can resume later by launching it again.",
                    "Exit Exam", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    // Ensure the absolute latest state is saved on manual exit
                    FileService.SaveProgress(GetProgressState());
                }
            }
        }
        
        /// <summary>
        /// Finishes the quiz, calculates score, and saves results
        /// </summary>
        private void FinishQuiz()
        {
            if (quizTimer != null)
            {
                quizTimer.Stop();
                quizTimer.Dispose();
                quizTimer = null;
            }
            
            SaveState();
            
            // Calculate score and stats
            int score = 0;
            int answeredCount = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                if (studentAnswers[i] != -1)
                {
                    answeredCount++;
                    if (questions[i].ShuffledOrder != null &&
                        questions[i].ShuffledOrder[studentAnswers[i]] == questions[i].CorrectIndex)
                    {
                        score++;
                    }
                }
            }
            
            int wrongCount = answeredCount - score;
            TimeSpan timeUsed = DateTime.Now - quizStartTime;
            
            // Show results
            using (var resultForm = new ResultForm(
                studentName,
                score,
                questions.Count,
                answeredCount,
                wrongCount,
                timeUsed,
                questions,
                studentAnswers))
            {
                resultForm.ShowDialog();
            }
            
            // Save results using FileService
            FileService.SaveResult(studentName, studentClass, score, questions.Count);
            
            // Delete the progress file now that the exam is successfully finished
            FileService.DeleteProgress();
            
            Application.Exit();
        }
        
        /// <summary>
        /// Disposes resources including timer
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                base.Dispose(disposing);
                return;
            }
            
            if (quizTimer != null)
            {
                quizTimer.Stop();
                quizTimer.Dispose();
                quizTimer = null;
            }
            
            // Explicitly dispose of the image to prevent GDI+ memory leaks
            if (picQuestionImage != null && picQuestionImage.Image != null)
            {
                picQuestionImage.Image.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
