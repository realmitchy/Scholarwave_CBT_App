using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ScholarwaveCBTApp.UI;

namespace ScholarwaveCBTApp.Forms
{
		/// <summary>
		/// A small, always-on-top calculator with two-line display:
		/// - First line shows the full expression (e.g., 6*7)
		/// - Second line shows the evaluated result (e.g., 42)
		/// </summary>
	public class CalculatorForm : MaterialAppForm
	{
		private TextBox txtExpression;
		private TextBox txtResult;
		private TableLayoutPanel keypad;

		private string pendingOperator = string.Empty;
		private double accumulator = 0.0;
		private bool clearOnNextDigit = false;
			private bool hasInputSinceOperator = false;

		public CalculatorForm()
		{
			InitializeComponents();
		}

		private void InitializeComponents()
		{
			FormBorderStyle = FormBorderStyle.FixedToolWindow; // close button only
			Text = "Calculator";
			TopMost = true;
			StartPosition = FormStartPosition.Manual;
			Width = 280;
			Height = 360;
			ThemeManager.ApplyFormSurface(this);

			// Expression (top line)
			txtExpression = new TextBox
			{
				ReadOnly = true,
				TextAlign = HorizontalAlignment.Right,
				Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular),
				Left = 8,
				Top = 8,
				Width = ClientSize.Width - 16,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				BorderStyle = BorderStyle.FixedSingle,
				Text = string.Empty
			};
			ThemeManager.StyleTextField(txtExpression);
			Controls.Add(txtExpression);
	
			// Result (second line, larger)
			txtResult = new TextBox
			{
				ReadOnly = true,
				TextAlign = HorizontalAlignment.Right,
				Font = new Font(Constants.DefaultFontFamily, 16F, FontStyle.Bold),
				Left = 8,
				Top = txtExpression.Bottom + 6,
				Width = ClientSize.Width - 16,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				BorderStyle = BorderStyle.FixedSingle,
				Text = "0"
			};
			ThemeManager.StyleTextField(txtResult);
			Controls.Add(txtResult);

			keypad = new TableLayoutPanel
			{
				Left = 8,
				Top = txtResult.Bottom + 10,
				Width = ClientSize.Width - 16,
				Height = ClientSize.Height - (txtResult.Bottom + 18),
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 4,
				RowCount = 5,
				BackColor = ThemeManager.Surface
			};
			for (int c = 0; c < 4; c++) keypad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			for (int r = 0; r < 5; r++) keypad.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
			Controls.Add(keypad);

			// Keys layout
			string[,] keys = new string[5, 4]
			{
				{ "C", "⌫", "±", "/" },
				{ "7", "8", "9", "*" },
				{ "4", "5", "6", "-" },
				{ "1", "2", "3", "+" },
				{ "0", ".", "=", "=" }
			};

			for (int r = 0; r < 5; r++)
			{
				for (int c = 0; c < 4; c++)
				{
					var label = keys[r, c];
					var btn = new Button
					{
						Text = label,
						Dock = DockStyle.Fill,
						Margin = new Padding(3),
						Font = new Font(Constants.DefaultFontFamily, 11F, FontStyle.Regular),
						FlatStyle = FlatStyle.Flat,
						BackColor = ThemeManager.SurfaceContainer,
						ForeColor = ThemeManager.OnSurface
					};
					btn.FlatAppearance.BorderColor = ThemeManager.Outline;
					btn.FlatAppearance.BorderSize = 1;
					btn.Click += Key_Click;
					keypad.Controls.Add(btn, c, r);
				}
			}
			
			UpdateDisplays();
		}

		private void Key_Click(object? sender, EventArgs e)
		{
			if (sender is not Button btn) return;
			string key = btn.Text;

			if (double.TryParse(key, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
			{
				AppendDigit(key);
				return;
			}

			switch (key)
			{
				case ".":
					AppendDecimalPoint();
					break;
				case "C":
					ClearAll();
					break;
				case "⌫":
					Backspace();
					break;
				case "±":
					ToggleSign();
					break;
				case "+":
				case "-":
				case "*":
				case "/":
					ApplyOperator(key);
					break;
				case "=":
					CalculateEquals();
					break;
			}
		}

		private void AppendDigit(string digit)
		{
			if (clearOnNextDigit)
			{
				txtResult.Text = "0";
				clearOnNextDigit = false;
				hasInputSinceOperator = false;
			}
			if (txtResult.Text == "0")
			{
				txtResult.Text = digit;
			}
			else
			{
				txtResult.Text += digit;
			}
			hasInputSinceOperator = true;
			UpdateDisplays();
		}

		private void AppendDecimalPoint()
		{
			if (clearOnNextDigit)
			{
				txtResult.Text = "0";
				clearOnNextDigit = false;
				hasInputSinceOperator = false;
			}
			if (!txtResult.Text.Contains("."))
			{
				txtResult.Text += ".";
			}
			UpdateDisplays();
		}

		private void ClearAll()
		{
			txtExpression.Text = string.Empty;
			txtResult.Text = "0";
			accumulator = 0.0;
			pendingOperator = string.Empty;
			clearOnNextDigit = false;
			hasInputSinceOperator = false;
			UpdateDisplays();
		}

		private void Backspace()
		{
			if (clearOnNextDigit) return;
			if (txtResult.Text.Length <= 1)
			{
				txtResult.Text = "0";
			}
			else
			{
				txtResult.Text = txtResult.Text.Substring(0, txtResult.Text.Length - 1);
			}
			UpdateDisplays();
		}

		private void ToggleSign()
		{
			if (txtResult.Text.StartsWith("-", StringComparison.Ordinal))
			{
				txtResult.Text = txtResult.Text.Substring(1);
			}
			else if (txtResult.Text != "0")
			{
				txtResult.Text = "-" + txtResult.Text;
			}
			UpdateDisplays();
		}

		private void ApplyOperator(string op)
		{
			double current = ParseCurrentInput();
			if (string.IsNullOrEmpty(pendingOperator))
			{
				accumulator = current;
			}
			else
			{
				accumulator = Evaluate(accumulator, current, pendingOperator);
				txtResult.Text = accumulator.ToString(CultureInfo.InvariantCulture);
			}
			pendingOperator = op;
			clearOnNextDigit = true;
			hasInputSinceOperator = false;
			UpdateDisplays();
		}

		private void CalculateEquals()
		{
			double current = ParseCurrentInput();
			if (!string.IsNullOrEmpty(pendingOperator))
			{
				accumulator = Evaluate(accumulator, current, pendingOperator);
				txtResult.Text = accumulator.ToString(CultureInfo.InvariantCulture);
				pendingOperator = string.Empty;
				clearOnNextDigit = true;
				hasInputSinceOperator = false;
				UpdateDisplays(finalize:true);
			}
		}

		private static double Evaluate(double left, double right, string op)
		{
			return op switch
			{
				"+" => left + right,
				"-" => left - right,
				"*" => left * right,
				"/" => right == 0 ? 0 : left / right,
				_ => right
			};
		}

		private double ParseCurrentInput()
		{
			if (double.TryParse(txtResult.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
			{
				return val;
			}
			return 0.0;
		}

		private void UpdateDisplays(bool finalize = false)
		{
			// Build expression string
			string expression;
			if (string.IsNullOrEmpty(pendingOperator))
			{
				expression = txtResult.Text == string.Empty ? "0" : txtResult.Text;
			}
			else
			{
				string left = accumulator.ToString(CultureInfo.InvariantCulture);
				string right = hasInputSinceOperator ? txtResult.Text : string.Empty;
				expression = string.IsNullOrEmpty(right)
					? $"{left}{pendingOperator}"
					: $"{left}{pendingOperator}{right}";
			}

			if (finalize && !string.IsNullOrEmpty(pendingOperator) == false)
			{
				// Show '=' after completing calculation
				expression = expression + " =";
			}

			txtExpression.Text = expression;

			// Live result: if we have an operator and a right operand, show evaluated value
			if (!string.IsNullOrEmpty(pendingOperator) && hasInputSinceOperator)
			{
				double right = ParseCurrentInput();
				double value = Evaluate(accumulator, right, pendingOperator);
				txtResult.Text = value.ToString(CultureInfo.InvariantCulture);
			}
			else if (string.IsNullOrEmpty(pendingOperator))
			{
				// txtResult already holds current input; no change
			}
		}
	}
}
