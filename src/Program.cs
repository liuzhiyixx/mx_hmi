using System;
using System.IO;
using System.Windows.Forms;

namespace MxHmi
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                ReportCrash(e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ReportCrash(e.ExceptionObject as Exception);
            };

            try
            {
                bool startInTray = HasArgument("--tray");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new WindowHostForm(startInTray));
            }
            catch (Exception ex)
            {
                ReportCrash(ex);
            }
        }

        private static void ReportCrash(Exception ex)
        {
            string message = ex == null ? "Unknown error" : ex.ToString();
            try
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), message);
            }
            catch
            {
            }

            MessageBox.Show(message, "MX HMI 启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static bool HasArgument(string expected)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                if (String.Equals(args[i], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
