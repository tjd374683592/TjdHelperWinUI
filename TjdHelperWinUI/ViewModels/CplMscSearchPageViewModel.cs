using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class CplMscSearchPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly EverythingHelper _helper;

        public ObservableCollection<SearchResultItem> CplResults { get; } = new();
        public ObservableCollection<SearchResultItem> MscResults { get; } = new();

        private SearchResultItem? _selectedCplItem;
        public SearchResultItem? SelectedCplItem
        {
            get => _selectedCplItem;
            set
            {
                if (_selectedCplItem != value)
                {
                    _selectedCplItem = value;
                    OnPropertyChanged(nameof(SelectedCplItem));
                }
            }
        }

        private SearchResultItem? _selectedMscItem;
        public SearchResultItem? SelectedMscItem
        {
            get => _selectedMscItem;
            set
            {
                if (_selectedMscItem != value)
                {
                    _selectedMscItem = value;
                    OnPropertyChanged(nameof(SelectedMscItem));
                }
            }
        }

        private bool _isSearchingCpl;
        public bool IsSearchingCpl
        {
            get => _isSearchingCpl;
            private set
            {
                if (_isSearchingCpl != value)
                {
                    _isSearchingCpl = value;
                    OnPropertyChanged(nameof(IsSearchingCpl));
                }
            }
        }

        private bool _isSearchingMsc;
        public bool IsSearchingMsc
        {
            get => _isSearchingMsc;
            private set
            {
                if (_isSearchingMsc != value)
                {
                    _isSearchingMsc = value;
                    OnPropertyChanged(nameof(IsSearchingMsc));
                }
            }
        }

        public ICommand OpenCplDirectoryCommand { get; }
        public ICommand ExecuteCplCommand { get; }
        public ICommand OpenMscDirectoryCommand { get; }
        public ICommand ExecuteMscCommand { get; }

        private static readonly Dictionary<string, string> MscDescriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["azman.msc"] = "Authorization Manager — 管理授权存储",
            ["certlm.msc"] = "Certificates (Local Computer) — 本地计算机证书",
            ["certmgr.msc"] = "Certificates — 当前用户证书",
            ["comexp.msc"] = "Component Services — 组件服务 (COM+)",
            ["compmgmt.msc"] = "Computer Management — 计算机管理",
            ["devmgmt.msc"] = "Device Manager — 设备管理器",
            ["diskmgmt.msc"] = "Disk Management — 磁盘管理",
            ["dsa.msc"] = "Active Directory Users and Computers — AD 用户与计算机",
            ["dssite.msc"] = "Active Directory Sites and Services — AD 站点与服务",
            ["dnsmgmt.msc"] = "DNS Manager — DNS 管理",
            ["dhcpmgmt.msc"] = "DHCP Management — DHCP 管理",
            ["eventvwr.msc"] = "Event Viewer — 事件查看器",
            ["fsmgmt.msc"] = "Shared Folders — 共享文件夹管理",
            ["gpedit.msc"] = "Group Policy Editor — 本地组策略编辑器",
            ["gpmc.msc"] = "Group Policy Management Console — 组策略管理控制台",
            ["lusrmgr.msc"] = "Local Users and Groups — 本地用户与组",
            ["perfmon.msc"] = "Performance Monitor — 性能监视器",
            ["printmanagement.msc"] = "Print Management — 打印管理",
            ["rsop.msc"] = "Resultant Set of Policy — 策略结果集",
            ["secpol.msc"] = "Local Security Policy — 本地安全策略",
            ["services.msc"] = "Services — 服务管理器",
            ["taskschd.msc"] = "Task Scheduler — 任务计划程序",
            ["tpm.msc"] = "TPM Management — 可信平台模块管理",
            ["wf.msc"] = "Windows Firewall with Advanced Security — 高级安全 Windows 防火墙",
            ["wmimgmt.msc"] = "WMI Control — Windows 管理工具 (WMI)",
            ["adfs.msc"] = "Active Directory Federation Services — ADFS 管理",
            ["adsiedit.msc"] = "ADSI Edit — Active Directory 编辑工具",
            ["certsrv.msc"] = "Certification Authority — 证书颁发机构管理",
            ["dns.msc"] = "DNS - 管理控制台 (备用名称)",
            ["virtmgmt.msc"] = "Hyper-V Manager — 虚拟化管理",
            ["wsus.msc"] = "Windows Server Update Services — WSUS 更新服务",
            ["wbadmin.msc"] = "Windows Server Backup — 服务器备份工具",
            ["scw.msc"] = "Security Configuration Wizard — 安全配置向导",
            ["nps.msc"] = "Network Policy Server — 网络策略服务器",
            ["wse.msc"] = "Windows Server Essentials 控制台",
            ["vss.msc"] = "Volume Shadow Copy 管理 (影子复制管理)",
            ["DevModeRunAsUserConfig.msc"] = "Developer Mode Run-As User Configuration — 开发者模式用户配置",
            ["iis.msc"] = "Internet Information Services (IIS) Manager — IIS 管理器",
            ["nfsmgmt.msc"] = "NFS 管理 — NFS 文件共享管理控制台"
        };

        public CplMscSearchPageViewModel()
        {
            _helper = new EverythingHelper();

            OpenCplDirectoryCommand = new RelayCommand(_ => OpenSelectedItemDirectory(SelectedCplItem));
            ExecuteCplCommand = new RelayCommand(_ => ExecuteSelectedItem(SelectedCplItem));
            OpenMscDirectoryCommand = new RelayCommand(_ => OpenSelectedItemDirectory(SelectedMscItem));
            ExecuteMscCommand = new RelayCommand(_ => ExecuteSelectedItem(SelectedMscItem));

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCplFilesAsync();
            await LoadMscFilesAsync();
        }

        #region 搜索 *.cpl
        private async Task LoadCplFilesAsync()
        {
            try
            {
                IsSearchingCpl = true;

                var results = await Task.Run(() =>
                {
                    _helper.EnsureEverythingRunning();

                    SearchResultItem[] items = Array.Empty<SearchResultItem>();
                    const int maxRetries = 10;
                    const int delayMs = 500;

                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            _helper.Search("*.cpl");
                            items = _helper.GetAllResults(500);

                            if (items.Length > 0)
                                break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Everything 查询 *.cpl 第 {attempt + 1} 次失败: {ex.Message}");
                        }

                        Thread.Sleep(delayMs);
                    }

                    return items;
                });

                var filtered = FilterAndPrioritizeResults(results);
                CplResults.Clear();
                int id = 1;
                foreach (var item in filtered)
                {
                    CplResults.Add(new SearchResultItem
                    {
                        Id = id++,
                        Name = item.Name.Trim(),
                        Directory = item.Directory.Trim(),
                        Size = item.Size,
                        DateModified = item.DateModified,
                        SearchResultType = item.SearchResultType,
                        Description = GetCplDescription(Path.Combine(item.Directory, item.Name))
                    });
                }
            }
            finally
            {
                IsSearchingCpl = false;
            }
        }

        private static string GetCplDescription(string path)
        {
            if (!File.Exists(path)) return string.Empty;
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                return !string.IsNullOrWhiteSpace(versionInfo.FileDescription)
                    ? versionInfo.FileDescription
                    : Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(path);
            }
        }
        #endregion

        #region 搜索 *.msc
        private async Task LoadMscFilesAsync()
        {
            try
            {
                IsSearchingMsc = true;

                var results = await Task.Run(() =>
                {
                    _helper.EnsureEverythingRunning();

                    SearchResultItem[] items = Array.Empty<SearchResultItem>();
                    const int maxRetries = 10;
                    const int delayMs = 500;

                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            _helper.Search("*.msc");
                            items = _helper.GetAllResults(500);

                            if (items.Length > 0)
                                break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Everything 查询 *.msc 第 {attempt + 1} 次失败: {ex.Message}");
                        }

                        Thread.Sleep(delayMs);
                    }

                    return items;
                });

                var filtered = FilterAndPrioritizeResults(results);
                MscResults.Clear();
                int id = 1;
                foreach (var item in filtered)
                {
                    string name = item.Name.Trim();
                    string fullPath = Path.Combine(item.Directory, name);
                    string description = MscDescriptions.TryGetValue(name, out var desc)
                        ? desc
                        : GetMscDescription(fullPath);

                    MscResults.Add(new SearchResultItem
                    {
                        Id = id++,
                        Name = name,
                        Directory = item.Directory.Trim(),
                        Size = item.Size,
                        DateModified = item.DateModified,
                        SearchResultType = item.SearchResultType,
                        Description = description
                    });
                }
            }
            finally
            {
                IsSearchingMsc = false;
            }
        }

        // 获取 MSC 文件自身描述
        private static string GetMscDescription(string path)
        {
            if (!File.Exists(path)) return string.Empty;

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                    return versionInfo.FileDescription;

                if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
                    return versionInfo.ProductName;

                return Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(path);
            }
        }
        #endregion


        #region 打开目录 / 执行
        private void OpenSelectedItemDirectory(SearchResultItem? item)
        {
            if (item == null) return;
            FileHelper.OpenFolder(Path.Combine(item.Directory, item.Name));
        }

        private void ExecuteSelectedItem(SearchResultItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Directory)) return;
            string fullPath = Path.Combine(item.Directory, item.Name);

            try
            {
                if (item.Name.EndsWith(".cpl", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        Arguments = $"\"{fullPath}\"",
                        UseShellExecute = true
                    });
                }
                else if (item.Name.EndsWith(".msc", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "mmc.exe",
                        Arguments = $"\"{fullPath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("执行失败", ex.Message);
            }
        }
        #endregion

        #region 去重 + 优先级
        private static ObservableCollection<SearchResultItem> FilterAndPrioritizeResults(IEnumerable<SearchResultItem> results)
        {
            var valid = results
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.SearchResultType == SearchResultItemType.File)
                .ToList();

            var grouped = valid
                .GroupBy(r => r.Name.ToLowerInvariant())
                .Select(g =>
                {
                    var sys32 = g.FirstOrDefault(x => x.Directory.Equals(@"C:\Windows\System32", StringComparison.OrdinalIgnoreCase));
                    if (sys32 != null) return sys32;

                    var syswow = g.FirstOrDefault(x => x.Directory.Equals(@"C:\Windows\SysWOW64", StringComparison.OrdinalIgnoreCase));
                    if (syswow != null) return syswow;

                    return g.First();
                })
                .OrderBy(r => r.Name);

            return new ObservableCollection<SearchResultItem>(grouped);
        }
        #endregion
    }
}
