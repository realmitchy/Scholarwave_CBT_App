using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScholarwaveCBTApp.UI
{
    /// <summary>
    /// Text field with rounded corners and theme-aware border/background.
    /// </summary>
    public class RoundedTextBox : Panel
    {
        private readonly TextBox _inner;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [Browsable(true)]
        public override string Text
        {
            get => _inner.Text;
            set => _inner.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public char PasswordChar
        {
            get => _inner.PasswordChar;
            set => _inner.PasswordChar = value;
        }

        public RoundedTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Padding = new Padding(12, 10, 12, 10);
            Height = 40;

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font(Constants.DefaultFontFamily, 10F, FontStyle.Regular)
            };
            _inner.GotFocus += (s, e) => Invalidate();
            _inner.LostFocus += (s, e) => Invalidate();
            Controls.Add(_inner);

            ApplyThemeColors();
            Resize += (s, e) => UpdateClipRegion();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyThemeColors();
            UpdateClipRegion();
        }

        public new event KeyPressEventHandler? KeyPress
        {
            add => _inner.KeyPress += value;
            remove => _inner.KeyPress -= value;
        }

        public void ApplyThemeColors()
        {
            BackColor = ThemeManager.SurfaceContainer;
            _inner.BackColor = ThemeManager.SurfaceContainer;
            _inner.ForeColor = ThemeManager.OnSurface;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _inner.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var fill = new SolidBrush(ThemeManager.SurfaceContainer);
            using var path = CreateRoundRect(rect, CornerRadius);
            e.Graphics.FillPath(fill, path);

            Color borderColor = _inner.Focused ? ThemeManager.Primary : ThemeManager.Outline;
            int borderWidth = _inner.Focused ? 2 : 1;
            using var pen = new Pen(borderColor, borderWidth);
            e.Graphics.DrawPath(pen, path);
        }

        private void UpdateClipRegion()
        {
            using GraphicsPath path = CreateRoundRect(new Rectangle(0, 0, Width, Height), CornerRadius);
            Region = new Region(path);
        }

        private static GraphicsPath CreateRoundRect(Rectangle bounds, int radius)
        {
            int arc = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new GraphicsPath();
            if (arc <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.X, bounds.Y, arc, arc, 180, 90);
            path.AddArc(bounds.Right - arc, bounds.Y, arc, arc, 270, 90);
            path.AddArc(bounds.Right - arc, bounds.Bottom - arc, arc, arc, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - arc, arc, arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
