using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QuizApp.Models;
using QuizApp.UI;

namespace QuizApp.Forms
{
    public class ResultReviewForm : MaterialAppForm
    {
        private readonly List<Question> _questions;
        private readonly int[] _studentAnswers;

        public ResultReviewForm(List<Question> questions, int[] studentAnswers)
        {
            _questions = questions ?? new List<Question>();
            _studentAnswers = studentAnswers ?? Array.Empty<int>();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "Result Review";
            Width = 900;
            Height = 650;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(780, 520);
            ThemeManager.ApplyFormSurface(this);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = ThemeManager.Surface
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            var scrollPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 0, 8, 0),
                BackColor = ThemeManager.Surface
            };

            for (int i = 0; i < _questions.Count; i++)
            {
                scrollPanel.Controls.Add(BuildQuestionCard(i));
            }

            var btnClose = new Button
            {
                Text = "Close",
                Width = 120,
                Height = 36,
                Anchor = AnchorStyles.Right
            };
            ThemeManager.StyleStandardButton(btnClose);
            btnClose.Click += (s, e) => Close();

            var btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface };
            btnPanel.Controls.Add(btnClose);
            btnPanel.Resize += (s, e) =>
            {
                btnClose.Left = btnPanel.ClientSize.Width - btnClose.Width - 4;
                btnClose.Top = (btnPanel.ClientSize.Height - btnClose.Height) / 2;
            };

            mainPanel.Controls.Add(scrollPanel, 0, 0);
            mainPanel.Controls.Add(btnPanel, 0, 1);

            Controls.Add(mainPanel);
        }

        private Control BuildQuestionCard(int questionIndex)
        {
            var question = _questions[questionIndex];
            var group = new GroupBox
            {
                Text = $"Question {questionIndex + 1}",
                Width = 830,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 12)
            };
            ThemeManager.StyleGroupBox(group);

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = ThemeManager.Surface
            };

            var lblQuestion = new Label
            {
                Text = question.Text,
                AutoSize = true,
                MaximumSize = new Size(790, 0),
                Font = new Font(Constants.DefaultFontFamily, 10.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            ThemeManager.StyleLabel(lblQuestion, isTitle: true);
            stack.Controls.Add(lblQuestion);

            int selectedDisplayIndex = (questionIndex < _studentAnswers.Length) ? _studentAnswers[questionIndex] : -1;

            for (int optionDisplayIndex = 0; optionDisplayIndex < 4; optionDisplayIndex++)
            {
                int actualOptionIndex = optionDisplayIndex;
                if (question.ShuffledOrder != null && question.ShuffledOrder.Count > optionDisplayIndex)
                {
                    actualOptionIndex = question.ShuffledOrder[optionDisplayIndex];
                }

                string optionText = actualOptionIndex >= 0 && actualOptionIndex < question.Options.Length
                    ? question.Options[actualOptionIndex]
                    : string.Empty;

                bool isCorrect = actualOptionIndex == question.CorrectIndex;
                bool isSelected = selectedDisplayIndex == optionDisplayIndex;
                string prefix = isCorrect ? "Correct: " : "Option: ";

                var optionLabel = new Label
                {
                    AutoSize = true,
                    MaximumSize = new Size(790, 0),
                    Margin = new Padding(10, 0, 0, 4),
                    Text = $"{prefix}{optionText}"
                };

                if (isCorrect)
                {
                    optionLabel.ForeColor = ThemeManager.CorrectAnswerText;
                    optionLabel.Font = new Font(Constants.DefaultFontFamily, 10f, FontStyle.Bold);
                }
                else
                {
                    ThemeManager.StyleLabel(optionLabel);
                }

                if (isSelected)
                {
                    optionLabel.Text += "  (Your answer)";
                    if (!isCorrect)
                    {
                        optionLabel.ForeColor = ThemeManager.WrongAnswerText;
                    }
                }

                stack.Controls.Add(optionLabel);
            }

            group.Controls.Add(stack);
            return group;
        }
    }
}
