using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace QuizApp.UI
{
    /// <summary>
    /// Central MaterialSkin.2 setup and control factories (M3-style purple scheme).
    /// </summary>
    public static class MaterialSkinService
    {
        private static bool _initialized;

        public static MaterialSkinManager Manager => MaterialSkinManager.Instance;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ApplyTheme();
        }

        public static void RegisterForm(MaterialForm form)
        {
            Initialize();
            Manager.AddFormToManage(form);
        }

        public static void ApplyTheme()
        {
            if (!_initialized)
                return;

            Manager.Theme = ThemeManager.IsDark
                ? MaterialSkinManager.Themes.DARK
                : MaterialSkinManager.Themes.LIGHT;

            // M3-inspired deep purple palette
            Manager.ColorScheme = ThemeManager.IsDark
                ? new ColorScheme(
                    Primary.DeepPurple500,
                    Primary.DeepPurple700,
                    Primary.DeepPurple800,
                    Accent.DeepPurple200,
                    TextShade.WHITE)
                : new ColorScheme(
                    Primary.DeepPurple600,
                    Primary.DeepPurple700,
                    Primary.DeepPurple100,
                    Accent.DeepPurple200,
                    TextShade.WHITE);
        }

        public static MaterialCard CreateCard(Padding? padding = null)
        {
            return new MaterialCard
            {
                BackColor = ThemeManager.SurfaceContainer,
                Padding = padding ?? new Padding(20),
                Margin = new Padding(16),
                Depth = 1
            };
        }

        public static MaterialSkin.Controls.MaterialButton CreateContainedButton(string text, int? width = null)
        {
            var button = new MaterialSkin.Controls.MaterialButton
            {
                Text = text,
                Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained,
                HighEmphasis = true,
                UseAccentColor = false,
                Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default,
                Depth = 0,
                MouseState = MaterialSkin.MouseState.HOVER,
                Height = 42
            };
            if (width.HasValue)
                button.Width = width.Value;
            return button;
        }

        public static MaterialSkin.Controls.MaterialButton CreateOutlinedButton(string text, int? width = null)
        {
            var button = new MaterialSkin.Controls.MaterialButton
            {
                Text = text,
                Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Outlined,
                HighEmphasis = false,
                Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default,
                Height = 40
            };
            if (width.HasValue)
                button.Width = width.Value;
            return button;
        }

        public static MaterialSkin.Controls.MaterialButton CreateTextButton(string text)
        {
            return new MaterialSkin.Controls.MaterialButton
            {
                Text = text,
                Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Text,
                HighEmphasis = false,
                Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default,
                Height = 36
            };
        }

        public static MaterialTextBox2 CreateTextField(string hint, bool isPassword = false)
        {
            var field = new MaterialTextBox2
            {
                Hint = hint,
                Depth = 0,
                MouseState = MaterialSkin.MouseState.OUT,
                AnimateReadOnly = false
            };
            if (isPassword)
                field.UseSystemPasswordChar = true;
            return field;
        }

        public static MaterialLabel CreateTitleLabel(string text)
        {
            return new MaterialLabel
            {
                Text = text,
                Depth = 0,
                FontType = MaterialSkinManager.fontType.H5,
                MouseState = MaterialSkin.MouseState.HOVER
            };
        }

        public static MaterialLabel CreateBodyLabel(string text)
        {
            return new MaterialLabel
            {
                Text = text,
                Depth = 0,
                FontType = MaterialSkinManager.fontType.Body1,
                MouseState = MaterialSkin.MouseState.HOVER
            };
        }

        public static MaterialSwitch CreateSwitch(string text, bool isChecked)
        {
            return new MaterialSwitch
            {
                Text = text,
                Checked = isChecked,
                Depth = 0,
                Margin = new Padding(4, 8, 4, 8),
                MouseLocation = new Point(-1, -1),
                MouseState = MaterialSkin.MouseState.HOVER
            };
        }

        public static bool IsMaterialControl(Control control) =>
            control is MaterialForm or MaterialCard or MaterialButton or MaterialTextBox2
                or MaterialLabel or MaterialSwitch or MaterialComboBox or MaterialCheckbox
                or MaterialRadioButton or MaterialDivider or MaterialListBox;
    }
}
