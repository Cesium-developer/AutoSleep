using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace AutoSleep.Settings
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // 管理员权限检查（与原版 Settings.ps1 一致）
            if (!IsAdministrator())
            {
                // 提权重启
                var proc = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                try
                {
                    Process.Start(proc);
                }
                catch
                {
                    // 用户取消提权
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
