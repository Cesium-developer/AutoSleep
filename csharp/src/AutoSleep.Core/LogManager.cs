using System;
using System.IO;

namespace AutoSleep.Core
{
    /// <summary>
    /// 日志写入
    /// </summary>
    public class LogManager
    {
        private const string LogFile = @"C:\ProgramData\AutoSleep\AutoSleep.log";

        public void Write(string message)
        {
            string line = string.Format("{0:HH:mm:ss} {1}", DateTime.Now, message);
            Console.WriteLine(line);

            try
            {
                string dir = Path.GetDirectoryName(LogFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
            catch { }
        }

        public void Clear()
        {
            try
            {
                if (File.Exists(LogFile))
                    File.Delete(LogFile);
            }
            catch { }
        }

        /// <summary>
        /// 日志轮转检查。
        /// 对照原版 PowerShell Invoke-LogRotation：
        /// - 用配置中的 LastRotationTime（而非文件创建时间）判断是否过期
        /// - 轮转后调用者负责更新 LastRotationTime 并保存配置
        /// 返回 true 表示轮转已执行。
        /// </summary>
        public bool RotateIfNeeded(int retentionDays, string lastRotationTime)
        {
            if (!File.Exists(LogFile)) return false;

            // 如果 LastRotationTime 为 null，初始化为文件创建时间
            string rotationTimeStr = lastRotationTime;
            if (string.IsNullOrEmpty(rotationTimeStr))
            {
                try
                {
                    var fileInfo = new FileInfo(LogFile);
                    rotationTimeStr = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    rotationTimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            // 解析时间
            DateTime lastRotation;
            if (!DateTime.TryParseExact(rotationTimeStr, "yyyy-MM-dd HH:mm:ss", null,
                System.Globalization.DateTimeStyles.None, out lastRotation))
            {
                // 解析失败则用当前时间
                lastRotation = DateTime.Now;
            }

            int ageDays = (int)(DateTime.Now - lastRotation).TotalDays;
            if (ageDays >= retentionDays)
            {
                try
                {
                    Write(string.Format("Log rotation triggered (age: {0} days, retention: {1} days).", ageDays, retentionDays));
                    File.Delete(LogFile);
                    Write("Log rotation: old log deleted.");
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}
