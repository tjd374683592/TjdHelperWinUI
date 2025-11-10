using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Pages
{
    public sealed partial class SystemCounterPage : Page
    {
        private const int MaxPoints = 60;
        private DateTime startTime;

        // CPU
        private LineSeries<ObservablePoint> cpuSeries;
        private Queue<ObservablePoint> cpuPoints = new Queue<ObservablePoint>();
        private PerformanceCounter cpuCounter;

        // 内存
        private LineSeries<ObservablePoint> memSeries;
        private Queue<ObservablePoint> memPoints = new Queue<ObservablePoint>();
        private PerformanceCounter memCounter;
        private ulong totalMemoryMB;

        // 磁盘
        private LineSeries<ObservablePoint> diskSeries;
        private Queue<ObservablePoint> diskPoints = new Queue<ObservablePoint>();
        private PerformanceCounter diskCounter;

        // 网络
        private LineSeries<ObservablePoint> netRxSeries;
        private LineSeries<ObservablePoint> netTxSeries;
        private Queue<ObservablePoint> netRxPoints = new Queue<ObservablePoint>();
        private Queue<ObservablePoint> netTxPoints = new Queue<ObservablePoint>();
        private PerformanceCounter netRxCounter;
        private PerformanceCounter netTxCounter;
        private string netInterfaceName;
        private double netMaxKB = 100; // 网络历史最大值

        // X 轴
        private Axis cpuXAxis, memXAxis, diskXAxis, netXAxis;

        // 网络 Y 轴
        private Axis netYAxis;

        public SystemCounterPage()
        {
            this.InitializeComponent();
            startTime = DateTime.Now;

            totalMemoryMB = GetTotalPhysicalMemoryMB();

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            memCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskCounter = new PerformanceCounter("LogicalDisk", "% Idle Time", "C:");

            netInterfaceName = GetNetworkInterfacePerfCounterName();
            netRxCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", netInterfaceName);
            netTxCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", netInterfaceName);

            // 初始化图表
            cpuSeries = CreateSeries("CPU", SKColors.LimeGreen);
            memSeries = CreateSeries("Memory (%)", SKColors.Orange);
            diskSeries = CreateSeries("Disk Time (%)", SKColors.Cyan);

            netRxSeries = new LineSeries<ObservablePoint>
            {
                Name = "Receive",
                Values = new List<ObservablePoint>(),
                Stroke = new SolidColorPaint(SKColors.Magenta, 2),
                GeometrySize = 0,
                LineSmoothness = 0.5,
                Fill = new SolidColorPaint(SKColors.Magenta.WithAlpha(50))
            };

            netTxSeries = new LineSeries<ObservablePoint>
            {
                Name = "Send",
                Values = new List<ObservablePoint>(),
                Stroke = new SolidColorPaint(SKColors.Cyan, 2)
                {
                    PathEffect = new DashEffect(new float[] { 8, 4 })
                },
                GeometrySize = 0,
                LineSmoothness = 0.5,
                Fill = null
            };

            CpuChart.Series = new ISeries[] { cpuSeries };
            MemChart.Series = new ISeries[] { memSeries };
            DiskChart.Series = new ISeries[] { diskSeries };
            NetChart.Series = new ISeries[] { netRxSeries, netTxSeries };

            // X 轴
            cpuXAxis = CreateXAxis();
            memXAxis = CreateXAxis();
            diskXAxis = CreateXAxis();
            netXAxis = CreateXAxis();

            CpuChart.XAxes = new Axis[] { cpuXAxis };
            MemChart.XAxes = new Axis[] { memXAxis };
            DiskChart.XAxes = new Axis[] { diskXAxis };
            NetChart.XAxes = new Axis[] { netXAxis };

            // Y 轴
            CpuChart.YAxes = new Axis[] { CreateYAxis("CPU (%)") };
            MemChart.YAxes = new Axis[] { CreateYAxis("Memory (%)") };
            DiskChart.YAxes = new Axis[] { CreateYAxis("Disk Time (%)") };

            netYAxis = new Axis
            {
                Name = "Network (KB/s)",
                MinLimit = 0,
                MaxLimit = netMaxKB * 1.1, // 初始最大值 +10%
                ShowSeparatorLines = true,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 1),
                Labeler = value => $"{value:F0} KB/s"
            };
            NetChart.YAxes = new Axis[] { netYAxis };

            StartMonitoring();
        }

        private LineSeries<ObservablePoint> CreateSeries(string name, SKColor color)
        {
            return new LineSeries<ObservablePoint>
            {
                Name = name,
                Values = new List<ObservablePoint>(),
                Stroke = new SolidColorPaint(color, 2),
                GeometrySize = 0,
                LineSmoothness = 0.5,
                Fill = new SolidColorPaint(color.WithAlpha(50))
            };
        }

        private Axis CreateXAxis()
        {
            return new Axis
            {
                ShowSeparatorLines = true,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 1),
                Labeler = value => startTime.AddSeconds(value).ToString("HH:mm:ss"),
                MinStep = 1
            };
        }

        private Axis CreateYAxis(string name)
        {
            return new Axis
            {
                Name = name,
                MinLimit = 0,
                MaxLimit = 100,
                ShowSeparatorLines = true,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 1),
                MinStep = 10
            };
        }

        private async void StartMonitoring()
        {
            double timeCounter = 0;
            while (true)
            {
                // CPU
                double cpuPercent = Math.Round(cpuCounter.NextValue(), 1);
                EnqueuePoint(cpuPoints, cpuSeries, timeCounter, cpuPercent);

                // 内存
                double memAvailableMB = memCounter.NextValue();
                double memPercent = Math.Round((totalMemoryMB - memAvailableMB) / totalMemoryMB * 100, 1);
                EnqueuePoint(memPoints, memSeries, timeCounter, memPercent);

                // 磁盘
                double diskIdle = diskCounter.NextValue();
                double diskActive = 100 - Math.Round(diskIdle, 1);
                diskActive = Math.Max(0, Math.Min(100, diskActive));
                EnqueuePoint(diskPoints, diskSeries, timeCounter, diskActive);

                // 网络 (Bytes -> KB)
                double netRxKB = netRxCounter.NextValue() / 1024;
                double netTxKB = netTxCounter.NextValue() / 1024;
                EnqueuePoint(netRxPoints, netRxSeries, timeCounter, netRxKB);
                EnqueuePoint(netTxPoints, netTxSeries, timeCounter, netTxKB);

                // 更新历史最大值
                double currentTotal = netRxKB + netTxKB;
                if (currentTotal > netMaxKB)
                    netMaxKB = currentTotal;

                netYAxis.MaxLimit = netMaxKB * 1.1; // 历史最大值 +10%

                // 顶部文字
                CpuText.Text = $"CPU: {cpuPercent}%";
                MemText.Text = $"Memory: {totalMemoryMB - memAvailableMB:F1}MB / {totalMemoryMB:F0}MB";
                DiskText.Text = $"磁盘活动时间: {diskActive:F1}%";
                NetText.Text = $"{netInterfaceName}: Rx {netRxKB:F1} KB/s / Tx {netTxKB:F1} KB/s";

                // X轴滚动
                UpdateXAxis(cpuXAxis, timeCounter);
                UpdateXAxis(memXAxis, timeCounter);
                UpdateXAxis(diskXAxis, timeCounter);
                UpdateXAxis(netXAxis, timeCounter);

                timeCounter++;
                await Task.Delay(1000);
            }
        }

        private void EnqueuePoint(Queue<ObservablePoint> queue, LineSeries<ObservablePoint> series, double x, double y)
        {
            queue.Enqueue(new ObservablePoint(x, y));
            if (queue.Count > MaxPoints)
                queue.Dequeue();
            series.Values = new List<ObservablePoint>(queue);
        }

        private void UpdateXAxis(Axis axis, double time)
        {
            axis.MinLimit = Math.Max(0, time - MaxPoints + 1);
            axis.MaxLimit = time;
        }

        private string GetNetworkInterfacePerfCounterName()
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instances = category.GetInstanceNames();

            foreach (var instance in instances)
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                            .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                                 instance.Contains(n.Name.Split(' ')[0]));
                if (nic != null)
                    return instance;
            }

            return instances.FirstOrDefault() ?? "以太网";
        }

        private ulong GetTotalPhysicalMemoryMB()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var totalBytes = (ulong)obj["TotalPhysicalMemory"];
                        return totalBytes / 1024 / 1024;
                    }
                }
            }
            catch
            {
                return 0;
            }
            return 0;
        }
    }
}
