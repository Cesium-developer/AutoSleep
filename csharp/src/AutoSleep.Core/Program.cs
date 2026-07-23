using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace AutoSleep.Core
{
    static class Program
    {
        static void Main()
        {
            // 管理员权限检查（原版 PowerShell 有 #Requires -RunAsAdministrator）
            if (!IsAdministrator())
            {
                var proc = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                try { Process.Start(proc); }
                catch { }
                return;
            }

            ConfigManager config = new ConfigManager();
            config.Load();

            MonitorEngine engine = new MonitorEngine(config);
            engine.Start();

            using (ManualResetEvent waitHandle = new ManualResetEvent(false))
            {
                waitHandle.WaitOne();
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }
}
