using System;
using System.Windows.Forms;
using DotNetEnv;
using ScholarwaveCBTApp.Forms;
using ScholarwaveCBTApp.UI;

namespace ScholarwaveCBTApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Load environment variables once at startup
            if (System.IO.File.Exists(".env"))
            {
                Env.Load();
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ThemeManager.Initialize();
            MaterialSkinService.Initialize();
			Application.Run(new StartModeForm());
        }
    }
}