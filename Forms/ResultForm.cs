using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
using QuizApp.Models;
using MaterialSkin.Controls;
using QuizApp.UI;

namespace QuizApp.Forms
{
    public class ResultForm : MaterialAppForm
    {
        private int _score;
        private int _totalQuestions;
        private int _answeredCount;
        private int _wrongCount;
        private TimeSpan _timeUsed;
        private string _studentName;
        private List<Question> _questions;
        private int[] _studentAnswers;

        // Result chart accent colors
        private readonly Color CorrectColor = Color.FromArgb(76, 175, 80); // Green
        private readonly Color FailColor = Color.FromArgb(244, 67, 54);    // Material Red
        private Color ChartTrackColor => ThemeManager.ChartTrack;
        private readonly Font HeaderFont = new Font("Segoe UI", 24, FontStyle.Bold);
        private readonly Font StatsFont = new Font("Segoe UI", 12);
        private readonly Font StatsBoldFont = new Font("Segoe UI", 12, FontStyle.Bold);
        private readonly Font PercentageFont = new Font("Segoe UI", 36, FontStyle.Bold);

        public ResultForm(string studentName, int score, int totalQuestions,
                          int answeredCount, int wrongCount, TimeSpan timeUsed,
                          List<Question> questions, int[] studentAnswers)
        {
            _studentName = studentName;
            _score = score;
            _totalQuestions = totalQuestions;
            _answeredCount = answeredCount;
            _wrongCount = wrongCount;
            _timeUsed = timeUsed;
            _questions = questions;
            _studentAnswers = studentAnswers;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Form Setup
            this.Text = "Quiz Results";
            this.Size = new Size(500, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new Size(500, 680);
            // Main Layout Panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20),
                BackColor = ThemeManager.Surface
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Header
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));   // Graph
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));   // Details
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Button

            // Header Label
            var lblHeader = new Label
            {
                Text = "Quiz Completed!",
                Font = HeaderFont,
                ForeColor = ThemeManager.OnSurface,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            mainPanel.Controls.Add(lblHeader, 0, 0);

            // Graph Panel (Custom Painting)
            var pnlGraph = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill
            };
            pnlGraph.Paint += PnlGraph_Paint;
            mainPanel.Controls.Add(pnlGraph, 0, 1);

            // Details Panel with stats
            var pnlDetails = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill
            };
            pnlDetails.Paint += PnlDetails_Paint;
            mainPanel.Controls.Add(pnlDetails, 0, 2);

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var btnViewResult = MaterialSkinService.CreateContainedButton("View Result", 150);
            btnViewResult.Height = 45;
            btnViewResult.Click += BtnViewResult_Click;

            var btnDone = MaterialSkinService.CreateOutlinedButton("Done", 150);
            btnDone.Height = 45;
            btnDone.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(btnViewResult);
            buttonPanel.Controls.Add(btnDone);
            buttonPanel.Resize += (s, e) =>
            {
                int spacing = 12;
                int totalWidth = btnViewResult.Width + spacing + btnDone.Width;
                int startX = Math.Max(0, (buttonPanel.ClientSize.Width - totalWidth) / 2);
                int y = Math.Max(0, (buttonPanel.ClientSize.Height - btnDone.Height) / 2);
                btnViewResult.Location = new Point(startX, y);
                btnDone.Location = new Point(startX + btnViewResult.Width + spacing, y);
            };
            mainPanel.Controls.Add(buttonPanel, 0, 3);

            this.Controls.Add(mainPanel);
        }

        private void PnlGraph_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var pnl = sender as Panel;
            if (pnl == null) return;

            int size = Math.Min(pnl.Width, pnl.Height) - 40;
            var rect = new Rectangle((pnl.Width - size) / 2, (pnl.Height - size) / 2, size, size);

            float percentage = _totalQuestions > 0 ? (float)_score / _totalQuestions : 0;
            float sweepAngle = 360f * percentage;

            // Choose arc color: red if < 30%, green otherwise
            Color arcColor = percentage < 0.30f ? FailColor : CorrectColor;

            // Draw Background Circle (Incorrect/Total)
            using (var pen = new Pen(ChartTrackColor, 40))
            {
                g.DrawArc(pen, rect, 0, 360);
            }

            // Draw Progress Arc
            if (_score > 0)
            {
                using (var pen = new Pen(arcColor, 40))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, rect, -90, sweepAngle);
                }
            }

            // Draw Percentage Text in Center
            string text = $"{(int)(percentage * 100)}%";
            SizeF textSize = g.MeasureString(text, PercentageFont);
            var textPoint = new PointF(
                rect.X + (rect.Width - textSize.Width) / 2,
                rect.Y + (rect.Height - textSize.Height) / 2);

            using (var brush = new SolidBrush(ThemeManager.OnSurface))
            {
                g.DrawString(text, PercentageFont, brush, textPoint);
            }
        }

        private void PnlDetails_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var pnl = sender as Panel;
            if (pnl == null) return;

            // Format time used
            string timeText;
            if (_timeUsed.TotalHours >= 1)
                timeText = $"{(int)_timeUsed.TotalHours}h {_timeUsed.Minutes:D2}m {_timeUsed.Seconds:D2}s";
            else if (_timeUsed.TotalMinutes >= 1)
                timeText = $"{(int)_timeUsed.TotalMinutes}m {_timeUsed.Seconds:D2}s";
            else
                timeText = $"{_timeUsed.Seconds}s";

            int unanswered = _totalQuestions - _answeredCount;

            string[] labels = {
                "Student:",
                "Time Used:",
                "Questions Answered:",
                "Correct:",
                "Wrong:",
                "Unanswered:"
            };
            string[] values = {
                _studentName,
                timeText,
                $"{_answeredCount} / {_totalQuestions}",
                $"{_score}",
                $"{_wrongCount}",
                $"{unanswered}"
            };

            float y = 8;
            float lineHeight = 26;
            float labelX = pnl.Width * 0.15f;
            float valueX = pnl.Width * 0.60f;

            using (var labelBrush = new SolidBrush(ThemeManager.Outline))
            using (var valueBrush = new SolidBrush(ThemeManager.OnSurface))
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    g.DrawString(labels[i], StatsFont, labelBrush, labelX, y);
                    g.DrawString(values[i], StatsBoldFont, valueBrush, valueX, y);
                    y += lineHeight;
                }
            }
        }

        private void BtnViewResult_Click(object? sender, EventArgs e)
        {
            using (var reviewForm = new ResultReviewForm(_questions, _studentAnswers))
            {
                reviewForm.ShowDialog(this);
            }
        }

        // Helper class to enable DoubleBuffered
        private class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                BackColor = ThemeManager.Surface;
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                              ControlStyles.UserPaint |
                              ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }
    }
}

