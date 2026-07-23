using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoSleep.Core
{
    /// <summary>
    /// 触发系统睡眠/休眠。
    /// 与 PowerShell 版行为完全一致。
    /// </summary>
    public class PowerManager
    {
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        public void Execute(string powerAction)
        {
            if (powerAction.Equals("Hibernate", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start("shutdown.exe", "/h");
            }
            else if (powerAction.Equals("Sleep", StringComparison.OrdinalIgnoreCase))
            {
                SetSuspendState(false, true, false);
            }
        }
    }
}
