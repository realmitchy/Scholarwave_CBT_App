using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QuizApp.Jamb;
using QuizApp.Models;
using QuizApp.UI;

namespace QuizApp.Forms
{
	public class JambQuizForm : MaterialAppForm
	{
		private JambExamSession session;
		private List<Button> subjectButtons = new List<Button>();
		private int activeSubjectIndex = 0;

		// Reuse many elements from QuizForm approach
		private Label lblTimer, lblTotalTime, lblQuestion;
		private ProgressBar prgTime;
		private RadioButton[] options;
		private Button btnNext, btnPrev, btnSubmit;
		private Button btnCalculator;
		private CheckBox chkFlag;
		private PictureBox picQuestionImage;
		private FlowLayoutPanel navPanel;
		private System.Windows.Forms.Timer? quizTimer;
		private CalculatorForm? calculatorForm;

		public JambQuizForm(JambExamSession jambSession)
		{
			session = jambSession;
			InitializeComponents();
			AttachHandlers();
			StartQuiz();
		}

		private void InitializeComponents()
		{
			Text = "JAMB Exam - 120 Minutes";
			Width = 1000;
			Height = 650;
			StartPosition = FormStartPosition.CenterScreen;
			FormBorderStyle = FormBorderStyle.Sizable;
			MinimumSize = new Size(960, 620);
			ThemeManager.ApplyFormSurface(this);

			// Subject buttons bar
			var subjBar = new FlowLayoutPanel
			{
				Left = 10,
				Top = 10,
				Width = 620,
				Height = 36,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				BackColor = ThemeManager.Surface
			};
			for (int i = 0; i < session.Subjects.Count; i++)
			{
				int idx = i;
				var b = new Button
				{
					Text = session.Subjects[i].SubjectTitle,
					Height = 32,
					AutoSize = true
				};
				ThemeManager.StyleStandardButton(b);
				b.Click += (s, e) =>
				{
					SaveState();
					activeSubjectIndex = idx;
					BuildNavButtons();
					ShowQuestion();
					UpdateSubjectButtons();
				};
				subjectButtons.Add(b);
				subjBar.Controls.Add(b);
			}

			// Timer/UI
			prgTime = new ProgressBar
			{
				Top = subjBar.Bottom + 4,
				Left = 10,
				Width = 620,
				Height = 14,
				Maximum = 120 * 60,
				Value = Math.Max(0, Math.Min(120 * 60, session.TotalSecondsRemaining))
			};
			lblTimer = new Label { Top = prgTime.Top - 2, Left = 570, Width = 80 };
			lblTotalTime = new Label
			{
				Text = "Time Remaining: ...",
				Top = prgTime.Bottom + 6,
				Left = 10,
				Width = 620
			};
			ThemeManager.StyleAccentLabel(lblTotalTime, italic: true);
			ThemeManager.StyleAccentLabel(lblTimer);

			// Question label
			lblQuestion = new Label
			{
				Top = lblTotalTime.Bottom + 6,
				Left = 10,
				Width = 620,
				Height = 90,
				Font = new Font(Constants.DefaultFontFamily, Constants.LargeFontSize, FontStyle.Bold)
			};
			ThemeManager.StyleLabel(lblQuestion, isTitle: true);

			// Image
			picQuestionImage = new PictureBox
			{
				Top = lblQuestion.Bottom + 6,
				Left = 10,
				Width = 620,
				Height = 200,
				SizeMode = PictureBoxSizeMode.Zoom,
				Visible = false
			};

			// Options
			options = new RadioButton[4];
			for (int i = 0; i < 4; i++)
			{
				options[i] = new RadioButton
				{
					Top = picQuestionImage.Bottom + 10 + i * 45,
					Left = 30,
					Width = 580,
					Font = new Font(Constants.DefaultFontFamily, 11F)
				};
				ThemeManager.StyleRadioButton(options[i]);
				Controls.Add(options[i]);
			}

			// Flag
			chkFlag = new CheckBox
			{
				Text = "Flag for Review",
				Top = options[3].Bottom + 12,
				Left = 30,
				Width = 150
			};
			ThemeManager.StyleCheckBox(chkFlag);

			// Nav buttons
			btnPrev = new Button { Text = "Back", Top = chkFlag.Bottom + 12, Left = 30, Width = 110, Height = 45 };
			ThemeManager.StyleStandardButton(btnPrev);
			IconTheme.ApplyToButton(btnPrev, AppIcons.ArrowBack);
			btnNext = new Button { Text = "Next", Top = chkFlag.Bottom + 12, Left = 150, Width = 110, Height = 45 };
			ThemeManager.StyleStandardButton(btnNext);
			IconTheme.ApplyToButton(btnNext, AppIcons.ArrowForward);
			btnSubmit = new Button
			{
				Text = "Submit All",
				Top = chkFlag.Bottom + 12,
				Left = 450,
				Width = 150,
				Height = 45
			};
			ThemeManager.StyleSubmitButton(btnSubmit);
			IconTheme.ApplyToButton(btnSubmit, AppIcons.Send, 18);

			// Calculator toggle button
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
			btnCalculator.Click += (s, e) => ToggleCalculator();

			// Question nav panel (per subject)
			navPanel = new FlowLayoutPanel
			{
				Top = 10,
				Left = 650,
				Width = 300,
				Height = 520,
				AutoScroll = true,
				BackColor = Constants.NavPanelBackColor,
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
			};

			Controls.AddRange(new Control[]
			{
				subjBar, prgTime, lblTimer, lblTotalTime, lblQuestion, picQuestionImage,
				chkFlag, btnPrev, btnNext, btnSubmit, navPanel, btnCalculator
			});

			btnCalculator.BringToFront();

			// Keep calculator pinned when window is moved or resized
			Move += (s, e) => RepositionCalculator();
			Resize += (s, e) => RepositionCalculator();
			ResizeEnd += (s, e) => RepositionCalculator();

			UpdateSubjectButtons();
		}

		private void AttachHandlers()
		{
			btnPrev.Click += (s, e) =>
			{
				SaveState();
				var subj = session.Subjects[activeSubjectIndex];
				if (subj.CurrentIndex > 0)
				{
					subj.CurrentIndex--;
					ShowQuestion();
				}
			};
			btnNext.Click += (s, e) =>
			{
				SaveState();
				var subj = session.Subjects[activeSubjectIndex];
				if (subj.CurrentIndex < subj.Questions.Count - 1)
				{
					subj.CurrentIndex++;
					ShowQuestion();
				}
			};
			btnSubmit.Click += (s, e) =>
			{
				var result = MessageBox.Show("Finalize and Submit all subjects?", "Confirm",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (result == DialogResult.Yes)
				{
					FinishExam();
				}
			};
			chkFlag.CheckedChanged += (s, e) =>
			{
				var subj = session.Subjects[activeSubjectIndex];
				subj.Flagged[subj.CurrentIndex] = chkFlag.Checked;
				UpdateNavColor();
			};
			FormClosing += (s, e) =>
			{
				if (quizTimer != null && quizTimer.Enabled)
				{
					var confirm = MessageBox.Show("Are you sure you want to stop the exam?",
						"Exit Exam", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if (confirm == DialogResult.No)
					{
						e.Cancel = true;
					}
				}
			};
		}

		private void StartQuiz()
		{
			UpdateTimerDisplay();
			quizTimer = new System.Windows.Forms.Timer { Interval = 1000 };
			quizTimer.Tick += (s, e) =>
			{
				session.TotalSecondsRemaining--;
				if (session.TotalSecondsRemaining < 0)
				{
					FinishExam();
					return;
				}
				UpdateTimerDisplay();
			};
			quizTimer.Start();
			BuildNavButtons();
			ShowQuestion();
		}

		private void UpdateTimerDisplay()
		{
			if (prgTime != null)
			{
				prgTime.Value = Math.Max(0, Math.Min(prgTime.Maximum, session.TotalSecondsRemaining));
			}
			var ts = TimeSpan.FromSeconds(session.TotalSecondsRemaining);
			lblTotalTime.Text = $"Time Remaining: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
			lblTimer.Text = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
		}

		private void BuildNavButtons()
		{
			navPanel.Controls.Clear();
			var subj = session.Subjects[activeSubjectIndex];
			for (int i = 0; i < subj.Questions.Count; i++)
			{
				int idx = i;
				var b = new Button
				{
					Text = (i + 1).ToString(),
					Width = 45,
					Height = 45
				};
				ThemeManager.StyleNavButton(b, Constants.UnansweredQuestionColor);
				b.Click += (s, e) =>
				{
					SaveState();
					subj.CurrentIndex = idx;
					ShowQuestion();
				};
				navPanel.Controls.Add(b);
			}
			UpdateNavColor();
		}

		private void UpdateNavColor()
		{
			var subj = session.Subjects[activeSubjectIndex];
			int count = Math.Min(navPanel.Controls.Count, subj.Questions.Count);
			for (int i = 0; i < count; i++)
			{
				if (navPanel.Controls[i] is not Button b) continue;
				if (subj.Flagged[i]) ThemeManager.StyleNavButton(b, Constants.FlaggedQuestionColor);
				else if (subj.StudentAnswers[i] != -1) ThemeManager.StyleNavButton(b, Constants.AnsweredQuestionColor);
				else ThemeManager.StyleNavButton(b, Constants.UnansweredQuestionColor);

				if (i == subj.CurrentIndex)
				{
					b.FlatStyle = FlatStyle.Flat;
					b.FlatAppearance.BorderSize = 3;
					b.FlatAppearance.BorderColor = Constants.ActiveQuestionBorderColor;
				}
				else
				{
					b.FlatStyle = FlatStyle.Standard;
					b.FlatAppearance.BorderSize = 1;
				}
			}
		}

		private void ShowQuestion()
		{
			var subj = session.Subjects[activeSubjectIndex];
			if (subj.Questions.Count == 0)
			{
				lblQuestion.Text = $"{subj.SubjectTitle} has no questions.";
				return;
			}

			if (subj.CurrentIndex < 0) subj.CurrentIndex = 0;
			if (subj.CurrentIndex >= subj.Questions.Count) subj.CurrentIndex = subj.Questions.Count - 1;

			var q = subj.Questions[subj.CurrentIndex];
			lblQuestion.Text = $"{subj.SubjectTitle} — Question {subj.CurrentIndex + 1} of {subj.Questions.Count}:\n{q.Text}";

			bool hasImage = !string.IsNullOrEmpty(q.ImagePath);
			if (hasImage)
			{
				try
				{
					string currentDir = AppDomain.CurrentDomain.BaseDirectory;
					string imageFullPath = System.IO.Path.Combine(currentDir, Constants.ImagesFolder, q.ImagePath);
					if (!System.IO.File.Exists(imageFullPath))
					{
						imageFullPath = System.IO.Path.Combine(currentDir, q.ImagePath);
					}
					if (System.IO.File.Exists(imageFullPath))
					{
						picQuestionImage.Image?.Dispose();
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
				catch
				{
					picQuestionImage.Visible = false;
				}
			}
			else
			{
				picQuestionImage.Visible = false;
				picQuestionImage.Image?.Dispose();
				picQuestionImage.Image = null;
			}

			int optionsStartY = picQuestionImage.Visible ? picQuestionImage.Bottom + 10 : lblQuestion.Bottom + 10;
			if (q.ShuffledOrder != null)
			{
				for (int i = 0; i < 4; i++)
				{
					options[i].Top = optionsStartY + (i * 45);
					options[i].Text = q.Options[q.ShuffledOrder[i]];
					options[i].Checked = (subj.StudentAnswers[subj.CurrentIndex] == i);
				}
			}

			int lastOptionBottom = options[3].Bottom;
			chkFlag.Top = lastOptionBottom + 12;
			btnPrev.Top = chkFlag.Bottom + 12;
			btnNext.Top = btnPrev.Top;
			btnSubmit.Top = btnPrev.Top;
			btnCalculator.Left = this.ClientSize.Width - btnCalculator.Width - 16;
			btnCalculator.Top = this.ClientSize.Height - btnCalculator.Height - 14;

			navPanel.Height = this.ClientSize.Height - 40;

			chkFlag.Checked = subj.Flagged[subj.CurrentIndex];
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

			var clientBottomRight = new Point(this.ClientSize.Width, this.ClientSize.Height);
			var screenPoint = this.PointToScreen(clientBottomRight);

			int marginX = 12;
			int marginY = 12;
			int calcX = screenPoint.X - calculatorForm.Width - marginX;
			int calcY = screenPoint.Y - calculatorForm.Height - marginY;

			calculatorForm.Location = new Point(Math.Max(0, calcX), Math.Max(0, calcY));
		}

		private void SaveState()
		{
			var subj = session.Subjects[activeSubjectIndex];
			subj.StudentAnswers[subj.CurrentIndex] = -1;
			for (int i = 0; i < 4; i++)
			{
				if (options[i].Checked)
				{
					subj.StudentAnswers[subj.CurrentIndex] = i;
				}
			}
			UpdateNavColor();
		}

		private void UpdateSubjectButtons()
		{
			for (int i = 0; i < subjectButtons.Count; i++)
			{
				ThemeManager.StyleStandardButton(subjectButtons[i]);
				subjectButtons[i].BackColor = (i == activeSubjectIndex)
					? ThemeManager.ActiveSubjectTab
					: ThemeManager.SecondaryControl;
			}
		}

		private void FinishExam()
		{
			if (quizTimer != null)
			{
				quizTimer.Stop();
				quizTimer.Dispose();
				quizTimer = null;
			}

			// Calculate per-subject and overall out of 400
			var perSubject = new List<(string title, int score, int total)>();
			int totalScore = 0;
			int totalQuestions = 0;
			int answeredCount = 0;
			var reviewQuestions = new List<Question>();
			var reviewAnswers = new List<int>();
			foreach (var subj in session.Subjects)
			{
				int score = 0;
				for (int i = 0; i < subj.Questions.Count; i++)
				{
					int ans = subj.StudentAnswers[i];
					reviewAnswers.Add(ans);

					// Clone question for cross-subject review and prefix title for clarity.
					var sourceQuestion = subj.Questions[i];
					reviewQuestions.Add(new Question
					{
						Text = $"[{subj.SubjectTitle}] {sourceQuestion.Text}",
						ImagePath = sourceQuestion.ImagePath,
						Options = sourceQuestion.Options,
						CorrectIndex = sourceQuestion.CorrectIndex,
						ShuffledOrder = sourceQuestion.ShuffledOrder
					});

					if (ans == -1) continue;
					answeredCount++;
					var q = subj.Questions[i];
					if (q.ShuffledOrder != null && q.ShuffledOrder[ans] == q.CorrectIndex)
					{
						score++;
					}
				}
				perSubject.Add((subj.SubjectTitle, score, subj.Questions.Count));
				totalScore += score;
				totalQuestions += subj.Questions.Count;
			}

			// Scale to 400
			int totalOutOf400 = totalQuestions > 0 ? (int)Math.Round(400.0 * totalScore / totalQuestions) : 0;

			int wrongCount = answeredCount - totalScore;
			TimeSpan timeUsed = DateTime.UtcNow - session.StartedAtUtc;

			using (var resultForm = new ResultForm(
				"JAMB Candidate",
				totalScore,
				totalQuestions,
				answeredCount,
				wrongCount,
				timeUsed,
				reviewQuestions,
				reviewAnswers.ToArray()))
			{
				resultForm.Text = $"JAMB Results (Scaled: {totalOutOf400}/400)";
				resultForm.ShowDialog();
			}

			Application.Exit();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && quizTimer != null)
			{
				quizTimer.Stop();
				quizTimer.Dispose();
				quizTimer = null;
			}

			if (disposing && calculatorForm != null && !calculatorForm.IsDisposed)
			{
				calculatorForm.Close();
				calculatorForm.Dispose();
				calculatorForm = null;
			}
			base.Dispose(disposing);
		}
	}
}
