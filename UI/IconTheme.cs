using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace QuizApp.UI
{
    /// <summary>
    /// Material Icons font theme for consistent UI glyphs across the app.
    /// </summary>
    public static class IconTheme
    {
        private const string FontFileName = "MaterialIcons-Regular.ttf";
        private static FontFamily? _iconFamily;
        private static Icon? _appIcon;

        // Codepoints for MaterialIcons-Regular.ttf (ligature names are not reliable in WinForms).
        private static readonly Dictionary<string, char> Codepoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["school"] = '\ue80c',
            ["menu_book"] = '\ue421',
            ["assignment"] = '\ue85d',
            ["lock"] = '\ue897',
            ["login"] = '\ue88a',
            ["person"] = '\ue7fd',
            ["calculate"] = '\ue41b',
            ["flag"] = '\ue153',
            ["brightness_2"] = '\ue3a9',
            ["brightness_7"] = '\ue3af',
            ["dark_mode"] = '\ue51c',
            ["light_mode"] = '\ue518',
            ["arrow_back"] = '\ue5c4',
            ["arrow_forward"] = '\ue5c8',
            ["check"] = '\ue5ca',
            ["send"] = '\ue163',
            ["refresh"] = '\ue5d5',
            ["add"] = '\ue145',
            ["edit"] = '\ue3c9',
            ["play_arrow"] = '\ue037',
            ["security"] = '\ue32a',
            ["help_outline"] = '\ue887',
        };

        public static string ResolveGlyph(string iconKey) =>
            Codepoints.TryGetValue(iconKey, out char code)
                ? code.ToString()
                : Codepoints["help_outline"].ToString();

        public static Font GetFont(float size, FontStyle style = FontStyle.Regular) =>
            new Font(GetFontFamily(), size, style, GraphicsUnit.Point);

        public static FontFamily GetFontFamily()
        {
            if (_iconFamily != null)
                return _iconFamily;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fontPath = Path.Combine(baseDir, "Assets", "Fonts", FontFileName);
            var collection = new PrivateFontCollection();

            if (!File.Exists(fontPath))
                throw new FileNotFoundException($"Icon font not found at {fontPath}");

            collection.AddFontFile(fontPath);
            _iconFamily = collection.Families[0];
            return _iconFamily;
        }

        public static Size MeasureGlyph(string iconKey, int size, Graphics g)
        {
            using var font = GetFont(size);
            string glyph = ResolveGlyph(iconKey);
            SizeF measured = g.MeasureString(glyph, font);
            return new Size((int)Math.Ceiling(measured.Width), (int)Math.Ceiling(measured.Height));
        }

        public static void DrawGlyph(Graphics g, string iconKey, int size, Color color, float x, float y)
        {
            using var font = GetFont(size);
            using var brush = new SolidBrush(color);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.DrawString(ResolveGlyph(iconKey), font, brush, x, y);
        }

        public static Bitmap RenderGlyph(string iconKey, int size, Color color)
        {
            string glyph = ResolveGlyph(iconKey);
            var bmp = new Bitmap(size + 8, size + 8);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DrawGlyph(g, iconKey, size, color, 2, 2);
            return bmp;
        }

        public static void ApplyToButton(Button button, string iconKey, int iconSize = 18)
        {
            button.Image?.Dispose();
            button.Image = null;
            button.TextImageRelation = TextImageRelation.Overlay;
            button.Padding = Padding.Empty;
            EnableUserPaint(button);
            button.Paint -= Button_PaintHandler;
            button.Tag = new IconButtonTag(iconKey, iconSize);
            button.Paint += Button_PaintHandler;
            button.Invalidate();
        }

        private static void EnableUserPaint(Control control)
        {
            const ControlStyles styles = ControlStyles.UserPaint |
                                         ControlStyles.AllPaintingInWmPaint |
                                         ControlStyles.OptimizedDoubleBuffer;
            typeof(Control).GetMethod(
                    "SetStyle",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(control, new object[] { styles, true });
        }

        public static void ClearButtonIcon(Button button)
        {
            button.Paint -= Button_PaintHandler;
            button.Tag = null;
            button.Image?.Dispose();
            button.Image = null;
            button.Invalidate();
        }

        private static void Button_PaintHandler(object? sender, PaintEventArgs e)
        {
            if (sender is not Button button || button.Tag is not IconButtonTag tag)
                return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(button.BackColor))
                e.Graphics.FillRectangle(bg, button.ClientRectangle);

            if (button.FlatStyle == FlatStyle.Flat && button.FlatAppearance.BorderSize > 0)
            {
                using var border = new Pen(button.FlatAppearance.BorderColor, button.FlatAppearance.BorderSize);
                e.Graphics.DrawRectangle(border, 0, 0, button.Width - 1, button.Height - 1);
            }

            const int leftPad = 10;
            const int gap = 8;
            using var iconFont = GetFont(tag.IconSize);
            string glyph = ResolveGlyph(tag.IconKey);
            SizeF iconSize = e.Graphics.MeasureString(glyph, iconFont);
            float iconY = (button.ClientSize.Height - iconSize.Height) / 2f;
            DrawGlyph(e.Graphics, tag.IconKey, tag.IconSize, button.ForeColor, leftPad, iconY);

            int textLeft = leftPad + (int)Math.Ceiling(iconSize.Width) + gap;
            var textRect = new Rectangle(textLeft, 0, button.ClientSize.Width - textLeft - 6, button.ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                button.Text,
                button.Font,
                textRect,
                button.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        public static Label CreateGlyphLabel(string iconKey, int size, Color? color = null)
        {
            return new Label
            {
                AutoSize = true,
                Font = GetFont(size),
                ForeColor = color ?? ThemeManager.OnSurface,
                BackColor = Color.Transparent,
                Text = ResolveGlyph(iconKey),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        public static Icon GetAppIcon()
        {
            if (_appIcon != null)
                return _appIcon;

            using Bitmap source = RenderGlyph(AppIcons.School, 48, ThemeManager.Primary);
            using var resized = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, 32, 32);
            }
            _appIcon = Icon.FromHandle(resized.GetHicon());
            return _appIcon;
        }

        public static void ApplyFormIcon(Form form)
        {
            try
            {
                form.Icon = GetAppIcon();
            }
            catch
            {
                // Keep default icon if font is unavailable
            }
        }

        private sealed class IconButtonTag
        {
            public IconButtonTag(string iconKey, int iconSize)
            {
                IconKey = iconKey;
                IconSize = iconSize;
            }

            public string IconKey { get; }
            public int IconSize { get; }
        }
    }
}
