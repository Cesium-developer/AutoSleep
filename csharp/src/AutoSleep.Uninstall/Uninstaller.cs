using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AutoSleep.Uninstall
{
    /// <summary>
    /// 卸载程序。
    /// 对应 PowerShell 版 Uninstall-AutoSleep.ps1 全部逻辑。
    /// 编译输出 Uninstall.exe，由 NSIS 打包。
    /// </summary>
    static class Uninstaller
    {
        private const string InstallDir = @"C:\ProgramData\AutoSleep";
        private const string TaskName = "AutoSleep";
        private const string RegPath64 = @"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep";
        private const string RegPathPs = @"HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep";

        static void Main()
        {
            Console.WriteLine("正在卸载 AutoSleep...");

            // 1. 结束所有 AutoSleep 进程
            foreach (var proc in Process.GetProcessesByName("AutoSleep"))
            {
                try { proc.Kill(); } catch { }
            }
            foreach (var proc in Process.GetProcessesByName("AutoSleepSettings"))
            {
                try { proc.Kill(); } catch { }
            }
            foreach (var proc in Process.GetProcessesByName("AutoSleepServer"))
            {
                try { proc.Kill(); } catch { }
            }
            Console.WriteLine("已结束后台进程");

            // 2. 删除桌面快捷方式
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutPath = Path.Combine(desktopPath, "AutoSleep 设置.lnk");
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
                Console.WriteLine("已删除桌面快捷方式");
            }

            // 3. 删除计划任务
            DeleteScheduledTask(TaskName);
            Console.WriteLine("已删除计划任务");

            // 4. 删除注册表（64位和32位视图）
            Console.WriteLine("正在删除注册表项...");
            RunReg("delete \"" + RegPath64 + "\" /f /reg:64");
            RunReg("delete \"" + RegPath64 + "\" /f /reg:32");

            // 检查是否删除成功
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep"))
                {
                    if (key != null)
                    {
                        Console.WriteLine("64 位注册表项删除失败，尝试 PowerShell 删除...");
                        RunPowerShell("Remove-Item -Path '" + RegPathPs + "' -Recurse -Force -ErrorAction Stop");
                    }
                }
            }
            catch { }

            // 5. 删除安装目录中除 Uninstall.exe 自身外的所有文件
            if (Directory.Exists(InstallDir))
            {
                Console.WriteLine("正在清理安装目录...");
                foreach (string dir in Directory.GetDirectories(InstallDir))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
                foreach (string file in Directory.GetFiles(InstallDir))
                {
                    if (!file.EndsWith("Uninstall.exe", StringComparison.OrdinalIgnoreCase)
                        && !file.EndsWith("Uninstall.pdb", StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                Console.WriteLine("其他文件已删除");
            }

            // 6. 创建自删除批处理（删除 Uninstall.exe 和空目录）
            string batPath = Path.Combine(Path.GetTempPath(), "SelfDelete.bat");
            string batContent = "@echo off\r\n"
                + "del /f /q \"" + InstallDir + "\\Uninstall.exe\" 2>nul\r\n"
                + "rmdir /s /q \"" + InstallDir + "\" 2>nul\r\n"
                + "del /f /q \"" + batPath + "\" 2>nul\r\n";
            File.WriteAllText(batPath, batContent);

            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Console.WriteLine("卸载完成。");
        }

        private static void DeleteScheduledTask(string taskName)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/delete /tn \"" + taskName + "\" /f",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (proc != null) proc.WaitForExit();
            }
            catch { }
        }

        private static void RunReg(string args)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (proc != null) proc.WaitForExit();
            }
            catch { }
        }

        private static void RunPowerShell(string cmd)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (proc != null) proc.WaitForExit();
            }
            catch { }
        }
    }
}
