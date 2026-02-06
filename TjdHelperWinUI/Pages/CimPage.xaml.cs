using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CimPage : Page
    {
        public CimPage()
        {
            InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            this.Loaded += CimPage_Loaded;
        }

        private async void CimPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator())
            {
                //不是管理员权限启动
                bool confirm = await ShowDiag();
                if (confirm)
                {
                    RestartAsAdministrator();
                }
                else {
                    App.MainWindow.MainWindowNavigationFrame.Navigate(typeof(HomePage));
                }
            }
            else
            {
                //管理员权限启动
                CheckAndStartWinRMService();

                //data binding
                if (Content is FrameworkElement rootElement)
                {
                    // 从 DI 容器中获取 ViewModel
                    rootElement.DataContext = App.Services.GetService<CimPageViewModel>();
                }
            }
        }

        public static async Task<bool> ShowDiag()
        {
            IMessageService messageService = new MessageService();
            bool confirm = await messageService.ShowConfirmDialogAsync("该模块需要 管理员权限 使用", "确定要 run as admin 吗");

            return confirm;
        }

        // 检测是否为管理员
        static bool IsAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // 以管理员权限重新启动自己
        static void RestartAsAdministrator()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                UseShellExecute = true,
                Verb = "runas" // 以管理员权限启动
            };

            try
            {
                Process.Start(psi);
                // 退出当前应用
                Application.Current.Exit();
            }
            catch
            {
                NotificationHelper.Show("用户取消了管理员权限请求。");
            }
        }

        private void CheckAndStartWinRMService()
        {
            string serviceName = "WinRM"; // WinRM 服务名称

            try
            {
                using ServiceController sc = new ServiceController(serviceName);

                if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    NotificationHelper.Show($"{serviceName} 服务已启动。");
                }
            }
            catch (InvalidOperationException ex)
            {
                NotificationHelper.Show($"无法找到服务 {serviceName}，请确认服务名称是否正确。错误: {ex.Message}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                NotificationHelper.Show($"启动服务失败，可能需要管理员权限。错误: {ex.Message}");
            }
        }

        private void HeaderedTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var vm = sender.DataContext as CimPageViewModel;
            if (vm != null)
            {
                // 单选：取 AddedItems 的第一个
                if (args.AddedItems.Count > 0)
                {
                    vm.SelectedNamespace = args.AddedItems[0] as CimTreeNode;
                }
                else
                {
                    vm.SelectedNamespace = null;
                }
            }
        }

        private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 找到最近的 FrameworkElement
            if (e.OriginalSource is DependencyObject dep)
            {
                // 向上查找 DataContext 为 CimTreeNode 的元素
                CimTreeNode? node = null;
                FrameworkElement? fe = null;
                while (dep != null)
                {
                    if (dep is FrameworkElement f && f.DataContext is CimTreeNode n)
                    {
                        node = n;
                        fe = f;
                        break;
                    }
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (node != null && fe != null)
                {
                    var menu = new MenuFlyout();
                    var copyItem = new MenuFlyoutItem { Text = "Copy" };
                    copyItem.Click += (s, args) =>
                    {
                        var dp = new DataPackage();
                        dp.SetText(node.Name);
                        Clipboard.SetContent(dp);
                    };
                    menu.Items.Add(copyItem);

                    menu.ShowAt(fe, e.GetPosition(fe));
                    e.Handled = true;
                }
            }
        }
    }
}