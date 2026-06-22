using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace QuizApp.UI
{
    public class MaterialButton : Button
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 12;

        public MaterialButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Height = 40;
            Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Bold);
            UseVisualStyleBackColor = false;

            ApplyThemeColors();
            Resize += (s, e) => UpdateRegion();
            MouseEnter += (s, e) => BackColor = ControlPaint.Light(ThemeManager.Primary, 0.08f);
            MouseLeave += (s, e) => BackColor = ThemeManager.Primary;
            MouseDown += (s, e) => BackColor = ControlPaint.Dark(ThemeManager.Primary, 0.08f);
            MouseUp += (s, e) => BackColor = ThemeManager.Primary;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyThemeColors();
            UpdateRegion();
        }

        private void ApplyThemeColors()
        {
            BackColor = ThemeManager.Primary;
            ForeColor = ThemeManager.OnPrimary;
        }

        private void UpdateRegion()
        {
            using GraphicsPath path = new GraphicsPath();
            int arc = CornerRadius * 2;

            path.AddArc(0, 0, arc, arc, 180, 90);
            path.AddArc(Width - arc, 0, arc, arc, 270, 90);
            path.AddArc(Width - arc, Height - arc, arc, arc, 0, 90);
            path.AddArc(0, Height - arc, arc, arc, 90, 90);
            path.CloseAllFigures();

            Region = new Region(path);
        }
    }
}
