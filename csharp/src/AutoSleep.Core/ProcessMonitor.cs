using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoSleep.Core
{
    /// <summary>
    /// 进程白名单检测
    /// </summary>
    public class ProcessMonitor
    {
        public bool Check(List<string> protectedProcesses)
        {
            if (protectedProcesses == null || protectedProcesses.Count == 0)
                return true;

            var runningProcesses = Process.GetProcesses();

            foreach (string pattern in protectedProcesses)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;

                foreach (var proc in runningProcesses)
                {
                    try
                    {
                        if (proc.ProcessName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                            return false;
                    }
                    catch { }
                }
            }

            return true;
        }

        /// <summary>
        /// 返回匹配的第一个进程名，若无匹配返回 null。
        /// 对应原版 PowerShell 的进程检查逻辑。
        /// </summary>
        public string FindRunning(List<string> protectedProcesses)
        {
            if (protectedProcesses == null || protectedProcesses.Count == 0)
                return null;

            var runningProcesses = Process.GetProcesses();

            foreach (string pattern in protectedProcesses)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;

                foreach (var proc in runningProcesses)
                {
                    try
                    {
                        if (proc.ProcessName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                            return proc.ProcessName;
                    }
                    catch { }
                }
            }

            return null;
        }
    }
}
