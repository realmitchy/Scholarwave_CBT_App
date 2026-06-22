using System;
using System.Windows.Forms;
using QuizApp;
using QuizApp.Forms;
using QuizApp.UI;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var failures = 0;
        void Check(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine($"OK  {name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL {name}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        Check("ThemeManager.Initialize", () => ThemeManager.Initialize());

        Check("StartModeForm (dark)", () =>
        {
            using var form = new StartModeForm();
            if (form.BackColor != ThemeManager.Surface)
                throw new InvalidOperationException("StartModeForm surface color mismatch.");
        });

        Check("LoginForm", () =>
        {
            using var form = new LoginForm();
            if (form.BackColor != ThemeManager.Surface)
                throw new InvalidOperationException("LoginForm surface color mismatch.");
        });

        Check("TeacherForm", () =>
        {
            using var form = new TeacherForm();
        });

        Check("JambModeForm", () =>
        {
            using var form = new JambModeForm();
        });

        Check("Theme toggle light", () =>
        {
            ThemeManager.SetMode(ThemeMode.Light, persist: false);
            using var form = new StartModeForm();
            if (ThemeManager.IsDark)
                throw new InvalidOperationException("Expected light mode after SetMode(Light).");
        });

        Check("Theme toggle dark", () =>
        {
            ThemeManager.SetMode(ThemeMode.Dark, persist: false);
            using var form = new LoginForm();
            if (!ThemeManager.IsDark)
                throw new InvalidOperationException("Expected dark mode after SetMode(Dark).");
        });

        Check("Constants theme colors", () =>
        {
            _ = Constants.AnsweredQuestionColor;
            _ = Constants.NavPanelBackColor;
            _ = Constants.SubmitButtonColor;
        });

        Console.WriteLine(failures == 0
            ? "Smoke verify passed."
            : $"Smoke verify failed ({failures} checks).");
        return failures == 0 ? 0 : 1;
    }
}
