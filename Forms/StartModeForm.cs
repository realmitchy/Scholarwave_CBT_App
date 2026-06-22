using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin.Controls;
using QuizApp.UI;

namespace QuizApp.Forms
{
	public class StartModeForm : MaterialAppForm
	{
		private MaterialLabel lblTitle;
		private MaterialLabel lblSubtitle;
		private MaterialSkin.Controls.MaterialButton btnJambMode;
		private MaterialSkin.Controls.MaterialButton btnQuizMode;
		private MaterialSwitch swDarkMode;
		private MaterialCard mainCard;

		public StartModeForm()
		{
			InitializeComponents();
		}

		private void InitializeComponents()
		{
			Text = "Choose Start Mode";
			Width = 680;
			Height = 420;
			StartPosition = FormStartPosition.CenterScreen;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;

			mainCard = MaterialSkinService.CreateCard(new Padding(28));
			mainCard.Dock = DockStyle.Fill;

			lblTitle = MaterialSkinService.CreateTitleLabel("Welcome");
			lblTitle.AutoSize = true;
			lblTitle.Location = new Point(8, 8);

			lblSubtitle = MaterialSkinService.CreateBodyLabel("Select a mode to get started");
			lblSubtitle.AutoSize = true;
			lblSubtitle.Location = new Point(8, 52);

			var buttonsPanel = new Panel
			{
				Location = new Point(8, 100),
				Size = new Size(560, 72),
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
			};

			btnJambMode = MaterialSkinService.CreateContainedButton("JAMB CBT Mode", 240);
			btnJambMode.Location = new Point(0, 12);
			btnJambMode.Click += (s, e) => OpenJambMode();

			btnQuizMode = MaterialSkinService.CreateContainedButton("Quiz Mode", 240);
			btnQuizMode.Location = new Point(256, 12);
			btnQuizMode.Click += (s, e) => OpenQuizMode();

			buttonsPanel.Controls.Add(btnJambMode);
			buttonsPanel.Controls.Add(btnQuizMode);

			swDarkMode = MaterialSkinService.CreateSwitch("Dark mode", ThemeManager.IsDark);
			swDarkMode.Location = new Point(8, 190);
			swDarkMode.CheckedChanged += (s, e) =>
			{
				ThemeManager.SetMode(swDarkMode.Checked ? ThemeMode.Dark : ThemeMode.Light, persist: true);
			};

			mainCard.Controls.Add(lblTitle);
			mainCard.Controls.Add(lblSubtitle);
			mainCard.Controls.Add(buttonsPanel);
			mainCard.Controls.Add(swDarkMode);
			Controls.Add(mainCard);
		}

		private void OpenJambMode()
		{
			Hide();
			var next = new JambModeForm();
			next.FormClosed += (s, e) => Close();
			next.Show();
		}

		private void OpenQuizMode()
		{
			Hide();
			var next = new LoginForm();
			next.FormClosed += (s, e) => Close();
			next.Show();
		}
	}
}
