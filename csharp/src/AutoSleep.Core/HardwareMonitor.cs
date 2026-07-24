using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace AutoSleep.Core
{
    /// <summary>
    /// CPU / GPU / 磁盘 / 网络 / 用户输入 采样数据
    /// </summary>
    public class HardwareData
    {
        public double CpuPercent { get; set; }
        public double GpuPercent { get; set; }
        public double DiskKBps { get; set; }
        public double NetworkKBps { get; set; }
        public uint IdleSeconds { get; set; }

        // 空闲判定（使用与 PowerShell 版相同的默认阈值）
        public bool CpuIdle { get { return CpuPercent < 30; } }
        public bool GpuIdle { get { return GpuPercent < 30; } }
        public bool DiskIdle { get { return DiskKBps < 10240; } }
        public bool NetworkIdle { get { return NetworkKBps < 1024; } }
        public bool UserIdle { get { return IdleSeconds > 3; } }
    }

    /// <summary>
    /// 硬件数据采集器
    /// 对应 PowerShell 版 Get-Counter 调用。
    /// GPU：先试系统计数器，失败则降级。
    /// 网络：汇总所有网卡，与 PowerShell 版行为一致。
    /// </summary>
    public class HardwareMonitor
    {
        private PerformanceCounter _cpuCounter;
        private List<PerformanceCounter> _networkCounters = new List<PerformanceCounter>();
        private PerformanceCounter _diskCounter;

        private bool _gpuFromCounter;
        private Dictionary<string, PerformanceCounter> _gpuCounterMap = new Dictionary<string, PerformanceCounter>();

        private bool _cpuReady;
        private bool _networkReady;
        private bool _diskReady;

        private uint _lastTick;
        private uint _idleSeconds;

        public HardwareMonitor()
        {
            InitCpuCounter();
            InitDiskCounter();
            InitNetworkCounters();
        }

        private void InitCpuCounter()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // 第一次总是 0，预热
                System.Threading.Thread.Sleep(100);
                _cpuCounter.NextValue();
                _cpuReady = true;
            }
            catch
            {
                _cpuReady = false;
            }
        }

        private void InitDiskCounter()
        {
            try
            {
                _diskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");
                _diskCounter.NextValue();
                _diskReady = true;
            }
            catch
            {
                _diskReady = false;
            }
        }

        private void InitNetworkCounters()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                string[] instances = category.GetInstanceNames();

                foreach (string inst in instances)
                {
                    // 跳过虚拟网卡（与 PowerShell 版行为一致）
                    if (inst.StartsWith("isatap", StringComparison.OrdinalIgnoreCase) ||
                        inst.StartsWith("Teredo", StringComparison.OrdinalIgnoreCase) ||
                        inst.StartsWith("6to4", StringComparison.OrdinalIgnoreCase) ||
                        inst.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                        inst.StartsWith("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                        inst.StartsWith("VMware", StringComparison.OrdinalIgnoreCase) ||
                        inst.StartsWith("Loopback", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var counter = new PerformanceCounter("Network Interface", "Bytes Total/sec", inst);
                        counter.NextValue();
                        _networkCounters.Add(counter);
                    }
                    catch { }
                }

                _networkReady = _networkCounters.Count > 0;
            }
            catch
            {
                _networkReady = false;
            }
        }

        public HardwareData Sample()
        {
            var data = new HardwareData();

            // CPU
            if (_cpuReady && _cpuCounter != null)
            {
                try { data.CpuPercent = Math.Round(_cpuCounter.NextValue(), 1); }
                catch { data.CpuPercent = 100; }
            }
            else
            {
                data.CpuPercent = 100; // 不可用时视为忙碌
            }

            // GPU — 每次 Sample() 刷新实例列表，跟 PowerShell 版行为一致
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                string[] currentInstances = category.GetInstanceNames();
                var instanceSet = new HashSet<string>(currentInstances);

                double maxGpu = 0;
                var staleKeys = new List<string>();

                // 读取已有计数器（已预热，数值准确）
                foreach (var kv in _gpuCounterMap)
                {
                    if (instanceSet.Contains(kv.Key))
                    {
                        try
                        {
                            double val = kv.Value.NextValue();
                            if (val > maxGpu) maxGpu = val;
                        }
                        catch { staleKeys.Add(kv.Key); }
                    }
                    else
                    {
                        staleKeys.Add(kv.Key);
                    }
                }

                // 清理失效的计数器
                foreach (var key in staleKeys)
                {
                    if (_gpuCounterMap.ContainsKey(key))
                    {
                        try { _gpuCounterMap[key].Dispose(); } catch { }
                        _gpuCounterMap.Remove(key);
                    }
                }

                // 添加新出现的实例（预热，下次 Sample 起效）
                foreach (string inst in currentInstances)
                {
                    if (!_gpuCounterMap.ContainsKey(inst))
                    {
                        try
                        {
                            var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", inst);
                            counter.NextValue();
                            _gpuCounterMap[inst] = counter;
                        }
                        catch { }
                    }
                }

                data.GpuPercent = Math.Round(maxGpu, 1);
                _gpuFromCounter = _gpuCounterMap.Count > 0;
            }
            catch
            {
                data.GpuPercent = 0;
                _gpuFromCounter = false;
            }

            // 磁盘
            if (_diskReady && _diskCounter != null)
            {
                try { data.DiskKBps = Math.Round(_diskCounter.NextValue() / 1024.0, 1); }
                catch { data.DiskKBps = 0; }
            }

            // 网络
            if (_networkReady && _networkCounters.Count > 0)
            {
                try
                {
                    double total = 0;
                    foreach (var c in _networkCounters)
                        total += c.NextValue();

                    data.NetworkKBps = Math.Round(total / 1024.0, 1);
                }
                catch { data.NetworkKBps = 0; }
            }

            // 用户输入
            data.IdleSeconds = GetIdleSeconds();

            return data;
        }

        #region 用户活动检测（从 PowerShell 版直接搬出）

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public uint GetIdleSeconds()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (GetLastInputInfo(ref lii))
            {
                uint tickCount = (uint)Environment.TickCount;
                return (tickCount - lii.dwTime) / 1000;
            }
            return 0;
        }

        #endregion
    }
}
