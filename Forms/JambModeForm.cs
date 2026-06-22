using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using QuizApp.Models;
using QuizApp.Jamb;

namespace QuizApp.Forms
{
	public class JambModeForm : Form
	{
		private ListBox lstAvailable;
		private ListBox lstChosen;
		private Button btnAdd;
		private Button btnRemove;
		private Button btnStart;
		private Label lblInfo;
		private List<QuizSet> availableQuizzes = new List<QuizSet>();

		public JambModeForm()
		{
			InitializeComponents();
			LoadAvailableQuizzes();
		}

		private void InitializeComponents()
		{
			Text = "JAMB CBT Mode - Select 4 Subjects";
			Width = 760;
			Height = 520;
			StartPosition = FormStartPosition.CenterScreen;
			FormBorderStyle = FormBorderStyle.Sizable;
			MinimumSize = new Size(740, 480);

			lblInfo = new Label
			{
				Text = "Pick exactly 4 subjects (quiz files). Time is fixed at 120 minutes.",
				Left = 16,
				Top = 16,
				Width = 700
			};

			lstAvailable = new ListBox { Left = 16, Top = 48, Width = 320, Height = 360 };
			lstChosen = new ListBox { Left = 408, Top = 48, Width = 320, Height = 360 };

			btnAdd = new Button
			{
				Text = "Add ▶",
				Left = 346,
				Top = 180,
				Width = 56,
				Height = 36
			};
			btnAdd.Click += (s, e) => MoveSelected(lstAvailable, lstChosen, max: 4);

			btnRemove = new Button
			{
				Text = "◀ Remove",
				Left = 346,
				Top = 226,
				Width = 56,
				Height = 36
			};
			btnRemove.Click += (s, e) => MoveSelected(lstChosen, lstAvailable, max: int.MaxValue);

			btnStart = new Button
			{
				Text = "Start JAMB Exam (120 mins)",
				Left = 408,
				Top = 420,
				Width = 320,
				Height = 40,
				BackColor = Color.MediumSeaGreen,
				ForeColor = Color.White,
				Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold)
			};
			btnStart.Click += BtnStart_Click;

			Controls.AddRange(new Control[] { lblInfo, lstAvailable, lstChosen, btnAdd, btnRemove, btnStart });
		}

		private void LoadAvailableQuizzes()
		{
			string baseDir = Directory.GetCurrentDirectory();
			string questionsDir = Path.Combine(baseDir, Constants.QuestionsFolder);
			if (!Directory.Exists(questionsDir))
				return;

			var files = Directory.GetFiles(questionsDir, "*.json");
			foreach (var file in files)
			{
				try
				{
					var json = File.ReadAllText(file);
					var quiz = JsonSerializer.Deserialize<QuizSet>(json);
					if (quiz != null && quiz.Questions != null && quiz.Questions.Count > 0)
					{
						availableQuizzes.Add(quiz);
					}
				}
				catch
				{
					// Ignore invalid quiz files
				}
			}

			lstAvailable.Items.Clear();
			foreach (var q in availableQuizzes)
			{
				lstAvailable.Items.Add(q.Title);
			}
		}

		private void MoveSelected(ListBox from, ListBox to, int max)
		{
			if (from.SelectedItem == null) return;

			if (to == lstChosen && to.Items.Count >= max)
			{
				MessageBox.Show("You can only choose 4 subjects.", "Limit Reached",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			var item = from.SelectedItem;
			from.Items.Remove(item);
			to.Items.Add(item);
		}

		private void BtnStart_Click(object? sender, EventArgs e)
		{
			if (lstChosen.Items.Count != 4)
			{
				MessageBox.Show("Please select exactly 4 subjects.", "Selection Required",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var chosenTitles = lstChosen.Items.Cast<string>().ToList();
			var chosenQuizzes = availableQuizzes.Where(q => chosenTitles.Contains(q.Title)).ToList();
			if (chosenQuizzes.Count != 4)
			{
				MessageBox.Show("Could not load selected subjects properly.", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Build session
			var session = new JambExamSession
			{
				TotalSecondsRemaining = 120 * 60,
				StartedAtUtc = DateTime.UtcNow,
				Subjects = chosenQuizzes.Select(q => new JambSubjectSession
				{
					SubjectTitle = q.Title,
					Questions = q.Questions.OrderBy(_ => Guid.NewGuid()).ToList(),
					CurrentIndex = 0,
					StudentAnswers = Enumerable.Repeat(-1, q.Questions.Count).ToArray(),
					Flagged = new bool[q.Questions.Count]
				}).ToList()
			};

			// Shuffle options for each question per subject
			foreach (var subj in session.Subjects)
			{
				foreach (var ques in subj.Questions)
				{
					ques.ShuffledOrder = Enumerable.Range(0, 4).OrderBy(_ => Guid.NewGuid()).ToList();
				}
			}

			this.Hide();
			var jambForm = new JambQuizForm(session);
			jambForm.FormClosed += (s2, e2) => this.Close();
			jambForm.Show();
		}
	}
}
