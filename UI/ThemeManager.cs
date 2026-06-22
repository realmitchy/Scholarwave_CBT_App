using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScholarwaveCBTApp.UI
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private const string ThemeSettingsFile = "theme.settings";

        public static ThemeMode CurrentMode { get; private set; } = ThemeMode.Dark;

        public static bool IsDark => CurrentMode == ThemeMode.Dark;

        // Material-like tokens (M3-inspired)
        public static Color Primary => IsDark
            ? Color.FromArgb(208, 188, 255)
            : Color.FromArgb(103, 80, 164);

        public static Color OnPrimary => IsDark
            ? Color.FromArgb(56, 30, 114)
            : Color.White;

        public static Color Surface => IsDark
            ? Color.FromArgb(20, 18, 24)
            : Color.FromArgb(254, 247, 255);

        public static Color SurfaceContainer => IsDark
            ? Color.FromArgb(33, 31, 38)
            : Color.FromArgb(243, 237, 247);

        public static Color OnSurface => IsDark
            ? Color.FromArgb(230, 225, 229)
            : Color.FromArgb(29, 27, 32);

        public static Color Outline => IsDark
            ? Color.FromArgb(147, 143, 153)
            : Color.FromArgb(121, 116, 126);

        public static Color Success => Color.FromArgb(76, 175, 80);
        public static Color Error => Color.FromArgb(244, 67, 54);
        public static Color TimerAccent => IsDark
            ? Color.FromArgb(255, 138, 128)
            : Color.FromArgb(180, 0, 0);

        public static Color SubmitButton => IsDark
            ? Color.FromArgb(180, 60, 75)
            : Color.FromArgb(220, 20, 60);

        public static Color OnSubmitButton => Color.White;

        public static Color AnsweredQuestion => IsDark
            ? Color.FromArgb(46, 94, 56)
            : Color.FromArgb(200, 230, 201);

        public static Color FlaggedQuestion => IsDark
            ? Color.FromArgb(120, 90, 20)
            : Color.FromArgb(255, 249, 196);

        public static Color UnansweredQuestion => IsDark
            ? Color.FromArgb(45, 43, 50)
            : Color.White;

        public static Color ActiveQuestionBorder => IsDark
            ? Primary
            : Color.FromArgb(25, 118, 210);

        public static Color NavPanelBackground => SurfaceContainer;

        public static Color SecondaryControl => IsDark
            ? Color.FromArgb(55, 52, 62)
            : SystemColors.Control;

        public static Color ActiveSubjectTab => IsDark
            ? Color.FromArgb(70, 65, 90)
            : Color.FromArgb(187, 222, 251);

        public static Color ChartTrack => IsDark
            ? Color.FromArgb(55, 52, 62)
            : Color.FromArgb(214, 205, 224);

        public static Color CorrectAnswerText => IsDark
            ? Color.FromArgb(129, 199, 132)
            : Color.ForestGreen;

        public static Color WrongAnswerText => IsDark
            ? Color.FromArgb(239, 154, 154)
            : Color.DarkRed;

        public static void Initialize()
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Trim();
                if (Enum.TryParse(text, true, out ThemeMode saved))
                {
                    SetMode(saved);
                    return;
                }
            }

            SetMode(DetectSystemTheme());
        }

        public static void SetMode(ThemeMode mode, bool persist = false)
        {
            CurrentMode = mode;
            if (persist)
            {
                SavePreference();
            }
            MaterialSkinService.ApplyTheme();
        }

        public static void ToggleMode(bool persist = true)
        {
            SetMode(IsDark ? ThemeMode.Light : ThemeMode.Dark, persist);
        }

        public static void SavePreference()
        {
            File.WriteAllText(GetSettingsPath(), CurrentMode.ToString());
        }

        private static string GetSettingsPath() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemeSettingsFile);

        private static ThemeMode DetectSystemTheme()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int useLight)
                {
                    return useLight == 0 ? ThemeMode.Dark : ThemeMode.Light;
                }
            }
            catch
            {
                // Fall through to default
            }

            return ThemeMode.Dark;
        }

        public static void ApplyFormSurface(Form form)
        {
            if (form is MaterialSkin.Controls.MaterialForm)
            {
                IconTheme.ApplyFormIcon(form);
                return;
            }

            form.BackColor = Surface;
            form.ForeColor = OnSurface;
            form.Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular);
            IconTheme.ApplyFormIcon(form);
        }

        public static void ApplyToForm(Form form)
        {
            ApplyFormSurface(form);
            ApplyToControls(form.Controls);
        }

        public static void RefreshForm(Form form)
        {
            ApplyFormSurface(form);
            ApplyToControls(form.Controls);
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                ApplyToControl(control);
                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls);
                }
            }
        }

        private static void ApplyToControl(Control control)
        {
            if (MaterialSkinService.IsMaterialControl(control))
                return;

            switch (control)
            {
                case TextBox textBox:
                    StyleTextField(textBox);
                    break;
                case Label label:
                    StyleLabel(label);
                    break;
                case RadioButton radioButton:
                    StyleRadioButton(radioButton);
                    break;
                case CheckBox checkBox:
                    StyleCheckBox(checkBox);
                    break;
                case ComboBox comboBox:
                    StyleComboBox(comboBox);
                    break;
                case ListBox listBox:
                    StyleListBox(listBox);
                    break;
                case GroupBox groupBox:
                    StyleGroupBox(groupBox);
                    break;
                case TableLayoutPanel tableLayoutPanel:
                    tableLayoutPanel.BackColor = Surface;
                    break;
                case FlowLayoutPanel flowLayoutPanel:
                    flowLayoutPanel.BackColor = Surface;
                    flowLayoutPanel.ForeColor = OnSurface;
                    break;
                case PictureBox:
                    break;
                case Panel panel:
                    StylePanel(panel);
                    break;
                case Button button:
                    if (button.Tag as string == "submit")
                    {
                        StyleSubmitButton(button);
                    }
                    else
                    {
                        StyleStandardButton(button);
                    }
                    break;
                case NumericUpDown numericUpDown:
                    StyleNumericUpDown(numericUpDown);
                    break;
            }
        }

        public static void StyleTextField(TextBox textBox)
        {
            textBox.BackColor = SurfaceContainer;
            textBox.ForeColor = OnSurface;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular);
        }

        public static void StyleLabel(Label label, bool isTitle = false)
        {
            label.ForeColor = OnSurface;
            label.BackColor = Color.Transparent;
            label.Font = isTitle
                ? new Font(Constants.DefaultFontFamily, 14F, FontStyle.Bold)
                : new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular);
        }

        public static void StyleRadioButton(RadioButton radioButton)
        {
            radioButton.ForeColor = OnSurface;
            radioButton.BackColor = Surface;
        }

        public static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.ForeColor = OnSurface;
            checkBox.BackColor = Surface;
        }

        public static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.BackColor = SurfaceContainer;
            comboBox.ForeColor = OnSurface;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular);
        }

        public static void StyleListBox(ListBox listBox)
        {
            listBox.BackColor = SurfaceContainer;
            listBox.ForeColor = OnSurface;
            listBox.BorderStyle = BorderStyle.FixedSingle;
            listBox.Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular);
        }

        public static void StylePanel(Panel panel)
        {
            panel.BackColor = Surface;
            panel.ForeColor = OnSurface;
        }

        public static void StyleGroupBox(GroupBox groupBox)
        {
            groupBox.ForeColor = OnSurface;
            groupBox.BackColor = Surface;
        }

        public static void StyleNumericUpDown(NumericUpDown numericUpDown)
        {
            numericUpDown.BackColor = SurfaceContainer;
            numericUpDown.ForeColor = OnSurface;
            numericUpDown.Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular);
        }

        public static void StyleStandardButton(Button button)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = SurfaceContainer;
            button.ForeColor = OnSurface;
            button.FlatAppearance.BorderColor = Outline;
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Regular);
        }

        public static void StyleSubmitButton(Button button)
        {
            button.Tag = "submit";
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = SubmitButton;
            button.ForeColor = OnSubmitButton;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font(Constants.DefaultFontFamily, Constants.DefaultFontSize, FontStyle.Bold);
        }

        public static void StyleNavButton(Button button, Color backColor)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Standard;
            button.BackColor = backColor;
            button.ForeColor = OnSurface;
        }

        public static void StyleAccentLabel(Label label, bool italic = false)
        {
            label.ForeColor = TimerAccent;
            label.BackColor = Color.Transparent;
            label.Font = new Font(
                Constants.DefaultFontFamily,
                Constants.DefaultFontSize,
                italic ? FontStyle.Italic : FontStyle.Regular);
        }
    }
}
