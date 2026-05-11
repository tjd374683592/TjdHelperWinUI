using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Pages
{
    public sealed partial class SystemCounterPage : Page
    {
        private const int MaxPoints = 60;
        private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

        private readonly DateTime startTime;
        private readonly CancellationTokenSource monitoringCancellationTokenSource = new();

        private readonly LineSeries<ObservablePoint> cpuSeries;
        private readonly Queue<ObservablePoint> cpuPoints = new();
        private readonly PerformanceCounter cpuCounter;

        private readonly LineSeries<ObservablePoint> memSeries;
        private readonly Queue<ObservablePoint> memPoints = new();
        private readonly PerformanceCounter memCounter;
        private readonly ulong totalMemoryMB;

        private readonly LineSeries<ObservablePoint> netRxSeries;
        private readonly LineSeries<ObservablePoint> netTxSeries;
        private readonly Queue<ObservablePoint> netRxPoints = new();
        private readonly Queue<ObservablePoint> netTxPoints = new();
        private readonly Axis cpuXAxis;
        private readonly Axis memXAxis;
        private readonly Axis netXAxis;
        private readonly Axis netYAxis;
        private readonly ObservableCollection<DriveCapacityItem> driveItems = new();
        private readonly SemaphoreSlim driveRefreshSemaphore = new(1, 1);

        private NetworkInterface? activeNetworkInterface;
        private long previousBytesReceived;
        private long previousBytesSent;
        private DateTime previousNetworkSampleTime;
        private bool hasNetworkBaseline;
        private double netMaxKB = 100;

        public SystemCounterPage()
        {
            InitializeComponent();

            startTime = DateTime.Now;
            totalMemoryMB = GetTotalPhysicalMemoryMB();

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            memCounter = new PerformanceCounter("Memory", "Available MBytes");

            cpuSeries = CreateSeries("CPU", SKColors.LimeGreen);
            memSeries = CreateSeries("Memory", SKColors.Orange);

            netRxSeries = new LineSeries<ObservablePoint>
            {
                Name = "Receive",
                Values = new List<ObservablePoint>(),
                Stroke = new SolidColorPaint(SKColors.Magenta, 2),
                GeometrySize = 0,
                LineSmoothness = 0.5,
                Fill = new SolidColorPaint(SKColors.Magenta.WithAlpha(45))
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
            NetChart.Series = new ISeries[] { netRxSeries, netTxSeries };

            cpuXAxis = CreateXAxis();
            memXAxis = CreateXAxis();
            netXAxis = CreateXAxis();

            CpuChart.XAxes = new Axis[] { cpuXAxis };
            MemChart.XAxes = new Axis[] { memXAxis };
            NetChart.XAxes = new Axis[] { netXAxis };

            CpuChart.YAxes = new Axis[] { CreatePercentYAxis("CPU (%)") };
            MemChart.YAxes = new Axis[] { CreatePercentYAxis("Memory (%)") };

            netYAxis = new Axis
            {
                Name = "Network (KB/s)",
                MinLimit = 0,
                MaxLimit = netMaxKB * 1.1,
                ShowSeparatorLines = true,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 1),
                Labeler = value => $"{value:F0}",
                MinStep = 10
            };
            NetChart.YAxes = new Axis[] { netYAxis };

            CpuText.Text = "初始化中...";
            MemText.Text = "初始化中...";
            NetText.Text = "初始化中...";
            DiskSummaryText.Text = "正在读取磁盘信息...";

            DriveListView.ItemsSource = driveItems;
            Unloaded += SystemCounterPage_Unloaded;

            _ = cpuCounter.NextValue();
            _ = RefreshDriveItemsAsync();
            _ = StartMonitoringAsync(monitoringCancellationTokenSource.Token);
        }

        private async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            double timeCounter = 0;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    double cpuPercent = Math.Clamp(Math.Round(cpuCounter.NextValue(), 1), 0, 100);
                    EnqueuePoint(cpuPoints, cpuSeries, timeCounter, cpuPercent);

                    double memAvailableMB = memCounter.NextValue();
                    double usedMemoryMB = totalMemoryMB > 0
                        ? Math.Max(0, totalMemoryMB - memAvailableMB)
                        : 0;
                    double memPercent = totalMemoryMB > 0
                        ? Math.Clamp(Math.Round(usedMemoryMB / totalMemoryMB * 100, 1), 0, 100)
                        : 0;
                    EnqueuePoint(memPoints, memSeries, timeCounter, memPercent);

                    NetworkSnapshot networkSnapshot = GetNetworkSnapshot();
                    EnqueuePoint(netRxPoints, netRxSeries, timeCounter, networkSnapshot.ReceiveKBPerSecond);
                    EnqueuePoint(netTxPoints, netTxSeries, timeCounter, networkSnapshot.SendKBPerSecond);

                    double currentTotal = networkSnapshot.ReceiveKBPerSecond + networkSnapshot.SendKBPerSecond;
                    if (currentTotal > netMaxKB)
                    {
                        netMaxKB = currentTotal;
                    }

                    netYAxis.MaxLimit = Math.Max(100, netMaxKB * 1.1);

                    CpuText.Text = $"当前占用 {cpuPercent:F1}%";
                    MemText.Text = totalMemoryMB > 0
                        ? $"已用 {FormatSizeText(usedMemoryMB * 1024 * 1024)} / {FormatSizeText(totalMemoryMB * 1024d * 1024)} ({memPercent:F1}%)"
                        : "内存信息不可用";
                    NetText.Text = networkSnapshot.IsAvailable
                        ? $"{networkSnapshot.InterfaceName}  Rx {networkSnapshot.ReceiveKBPerSecond:F1} KB/s  Tx {networkSnapshot.SendKBPerSecond:F1} KB/s"
                        : "未检测到可用网络数据，图表保留为 0";

                    UpdateXAxis(cpuXAxis, timeCounter);
                    UpdateXAxis(memXAxis, timeCounter);
                    UpdateXAxis(netXAxis, timeCounter);

                    if (((int)timeCounter % 30) == 0)
                    {
                        _ = RefreshDriveItemsAsync();
                    }

                    timeCounter++;
                    await Task.Delay(SampleInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RefreshDriveItemsAsync()
        {
            if (!await driveRefreshSemaphore.WaitAsync(0))
            {
                return;
            }

            try
            {
                var latestDriveItems = await Task.Run(() =>
                    DriveInfo.GetDrives()
                        .Where(drive => drive.IsReady)
                        .OrderBy(drive => drive.Name)
                        .Select(CreateDriveCapacityItem)
                        .ToList());

                ApplyDriveItems(latestDriveItems);
                DiskSummaryText.Text = latestDriveItems.Count > 0
                    ? $"已检测到 {latestDriveItems.Count} 个磁盘，右键可直接打开。"
                    : "未检测到可用磁盘。";
            }
            catch
            {
                driveItems.Clear();
                DiskSummaryText.Text = "磁盘信息读取失败。";
            }
            finally
            {
                driveRefreshSemaphore.Release();
            }
        }

        private void ApplyDriveItems(IReadOnlyList<DriveCapacityItem> latestDriveItems)
        {
            for (int i = 0; i < latestDriveItems.Count; i++)
            {
                if (i < driveItems.Count)
                {
                    driveItems[i].UpdateFrom(latestDriveItems[i]);
                }
                else
                {
                    driveItems.Add(latestDriveItems[i]);
                }
            }

            while (driveItems.Count > latestDriveItems.Count)
            {
                driveItems.RemoveAt(driveItems.Count - 1);
            }
        }

        private DriveCapacityItem CreateDriveCapacityItem(DriveInfo drive)
        {
            double totalBytes = drive.TotalSize;
            double freeBytes = drive.AvailableFreeSpace;
            double usedBytes = Math.Max(0, totalBytes - freeBytes);
            double usagePercent = totalBytes > 0 ? usedBytes / totalBytes * 100 : 0;

            string driveLetter = drive.Name.TrimEnd(Path.DirectorySeparatorChar);
            string volumeLabel = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "本地磁盘" : drive.VolumeLabel;

            return new DriveCapacityItem
            {
                RootPath = drive.RootDirectory.FullName,
                DisplayName = $"{volumeLabel} ({driveLetter})",
                UsagePercent = Math.Round(usagePercent, 1),
                UsagePercentText = $"{usagePercent:F0}%",
                FreeText = $"{FormatSizeText(freeBytes)} 可用",
                CapacityText = $"{FormatSizeText(totalBytes)} 总容量"
            };
        }

        private NetworkSnapshot GetNetworkSnapshot()
        {
            NetworkInterface? networkInterface = GetPreferredNetworkInterface();
            if (networkInterface == null)
            {
                ResetNetworkBaseline();
                return new NetworkSnapshot("未检测到活动网卡", 0, 0, false);
            }

            IPv4InterfaceStatistics? statistics = TryGetIPv4Statistics(networkInterface);
            if (statistics == null)
            {
                ResetNetworkBaseline();
                return new NetworkSnapshot(networkInterface.Name, 0, 0, false);
            }

            DateTime sampleTime = DateTime.UtcNow;
            double receiveKBPerSecond = 0;
            double sendKBPerSecond = 0;

            if (hasNetworkBaseline && activeNetworkInterface?.Id == networkInterface.Id)
            {
                double elapsedSeconds = Math.Max((sampleTime - previousNetworkSampleTime).TotalSeconds, 0.1);
                receiveKBPerSecond = Math.Max(0, (statistics.BytesReceived - previousBytesReceived) / elapsedSeconds / 1024d);
                sendKBPerSecond = Math.Max(0, (statistics.BytesSent - previousBytesSent) / elapsedSeconds / 1024d);
            }

            activeNetworkInterface = networkInterface;
            previousBytesReceived = statistics.BytesReceived;
            previousBytesSent = statistics.BytesSent;
            previousNetworkSampleTime = sampleTime;
            hasNetworkBaseline = true;

            return new NetworkSnapshot(GetNetworkDisplayName(networkInterface), receiveKBPerSecond, sendKBPerSecond, true);
        }

        private NetworkInterface? GetPreferredNetworkInterface()
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(IsUsableNetworkInterface)
                .Select(networkInterface => new
                {
                    Interface = networkInterface,
                    Properties = TryGetIPProperties(networkInterface),
                    Statistics = TryGetIPv4Statistics(networkInterface)
                })
                .Where(item => item.Statistics != null)
                .ToList();

            var activeMatch = candidates.FirstOrDefault(item => item.Interface.Id == activeNetworkInterface?.Id);
            if (activeMatch != null)
            {
                return activeMatch.Interface;
            }

            return candidates
                .OrderByDescending(item => HasGateway(item.Properties))
                .ThenByDescending(item => IsPreferredInterfaceType(item.Interface))
                .ThenByDescending(item => item.Statistics!.BytesReceived + item.Statistics.BytesSent)
                .Select(item => item.Interface)
                .FirstOrDefault();
        }

        private static bool IsUsableNetworkInterface(NetworkInterface networkInterface)
        {
            return networkInterface.OperationalStatus == OperationalStatus.Up
                && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Unknown;
        }

        private static bool IsPreferredInterfaceType(NetworkInterface networkInterface)
        {
            return networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                || networkInterface.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet
                || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
        }

        private static bool HasGateway(IPInterfaceProperties? properties)
        {
            return properties?.GatewayAddresses.Any(address => address?.Address != null) == true;
        }

        private static IPInterfaceProperties? TryGetIPProperties(NetworkInterface networkInterface)
        {
            try
            {
                return networkInterface.GetIPProperties();
            }
            catch
            {
                return null;
            }
        }

        private static IPv4InterfaceStatistics? TryGetIPv4Statistics(NetworkInterface networkInterface)
        {
            try
            {
                return networkInterface.GetIPv4Statistics();
            }
            catch
            {
                return null;
            }
        }

        private static string GetNetworkDisplayName(NetworkInterface networkInterface)
        {
            return string.IsNullOrWhiteSpace(networkInterface.Name)
                ? networkInterface.Description
                : networkInterface.Name;
        }

        private void ResetNetworkBaseline()
        {
            activeNetworkInterface = null;
            previousBytesReceived = 0;
            previousBytesSent = 0;
            previousNetworkSampleTime = default;
            hasNetworkBaseline = false;
        }

        private static LineSeries<ObservablePoint> CreateSeries(string name, SKColor color)
        {
            return new LineSeries<ObservablePoint>
            {
                Name = name,
                Values = new List<ObservablePoint>(),
                Stroke = new SolidColorPaint(color, 2),
                GeometrySize = 0,
                LineSmoothness = 0.5,
                Fill = new SolidColorPaint(color.WithAlpha(45))
            };
        }

        private Axis CreateXAxis()
        {
            return new Axis
            {
                ShowSeparatorLines = true,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 1),
                Labeler = value => startTime.AddSeconds(value).ToString("HH:mm:ss"),
                MinStep = 5
            };
        }

        private static Axis CreatePercentYAxis(string name)
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

        private static void EnqueuePoint(Queue<ObservablePoint> queue, LineSeries<ObservablePoint> series, double x, double y)
        {
            queue.Enqueue(new ObservablePoint(x, y));
            if (queue.Count > MaxPoints)
            {
                queue.Dequeue();
            }

            series.Values = new List<ObservablePoint>(queue);
        }

        private static void UpdateXAxis(Axis axis, double time)
        {
            axis.MinLimit = Math.Max(0, time - MaxPoints + 1);
            axis.MaxLimit = time;
        }

        private void DriveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not DriveCapacityItem drive)
            {
                return;
            }

            var menu = new MenuFlyout();
            var openItem = new MenuFlyoutItem { Text = $"打开 {drive.RootPath}" };
            openItem.Click += (_, _) => OpenDrive(drive.RootPath);
            menu.Items.Add(openItem);
            menu.ShowAt(element, e.GetPosition(element));
            e.Handled = true;
        }

        private static void OpenDrive(string rootPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(rootPath) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private void SystemCounterPage_Unloaded(object sender, RoutedEventArgs e)
        {
            monitoringCancellationTokenSource.Cancel();
            cpuCounter.Dispose();
            memCounter.Dispose();
            monitoringCancellationTokenSource.Dispose();
            Unloaded -= SystemCounterPage_Unloaded;
        }

        private static string FormatSizeText(double bytes)
        {
            const double KB = 1024;
            const double MB = KB * 1024;
            const double GB = MB * 1024;
            const double TB = GB * 1024;

            return bytes switch
            {
                >= TB => $"{bytes / TB:F1} TB",
                >= GB => $"{bytes / GB:F1} GB",
                >= MB => $"{bytes / MB:F1} MB",
                >= KB => $"{bytes / KB:F1} KB",
                _ => $"{bytes:F0} B"
            };
        }

        private ulong GetTotalPhysicalMemoryMB()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    ulong totalBytes = (ulong)obj["TotalPhysicalMemory"];
                    return totalBytes / 1024 / 1024;
                }
            }
            catch
            {
                return 0;
            }

            return 0;
        }

        public sealed class DriveCapacityItem : INotifyPropertyChanged
        {
            private string rootPath = string.Empty;
            private string displayName = string.Empty;
            private double usagePercent;
            private string usagePercentText = string.Empty;
            private string freeText = string.Empty;
            private string capacityText = string.Empty;

            public string RootPath
            {
                get => rootPath;
                init => rootPath = value;
            }

            public string DisplayName
            {
                get => displayName;
                set => SetField(ref displayName, value);
            }

            public double UsagePercent
            {
                get => usagePercent;
                set => SetField(ref usagePercent, value);
            }

            public string UsagePercentText
            {
                get => usagePercentText;
                set => SetField(ref usagePercentText, value);
            }

            public string FreeText
            {
                get => freeText;
                set => SetField(ref freeText, value);
            }

            public string CapacityText
            {
                get => capacityText;
                set => SetField(ref capacityText, value);
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            public void UpdateFrom(DriveCapacityItem latest)
            {
                DisplayName = latest.DisplayName;
                UsagePercent = latest.UsagePercent;
                UsagePercentText = latest.UsagePercentText;
                FreeText = latest.FreeText;
                CapacityText = latest.CapacityText;
            }

            private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                {
                    return;
                }

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private sealed record NetworkSnapshot(string InterfaceName, double ReceiveKBPerSecond, double SendKBPerSecond, bool IsAvailable);
    }
}
