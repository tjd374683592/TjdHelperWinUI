using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace TjdHelperWinUI.Pages
{
    public sealed partial class NetworkPage : Page
    {
        private DispatcherQueue _dispatcher;

        public NetworkPage()
        {
            InitializeComponent();

            _dispatcher = DispatcherQueue.GetForCurrentThread();

            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = App.Services.GetService<NetworkPageViewModel>();
            }

            UpdateNetworkUI();

            NetworkHelper.Instance.NetworkChanged += Instance_NetworkChanged;
            Unloaded += NetworkPage_Unloaded;
        }

        private void Instance_NetworkChanged(object? sender, System.EventArgs e)
        {
            _dispatcher.TryEnqueue(UpdateNetworkUI);
        }

        private void NetworkPage_Unloaded(object sender, RoutedEventArgs e)
        {
            NetworkHelper.Instance.NetworkChanged -= Instance_NetworkChanged;
        }

        private void UpdateNetworkUI()
        {
            var info = NetworkHelper.Instance.ConnectionInformation;

            IsInternetAvailableText.Text = info.IsInternetAvailable ? "Yes" : "No";
            IsInternetOnMeteredConnectionText.Text = info.IsInternetOnMeteredConnection ? "Yes" : "No";
            ConnectionTypeText.Text = info.ConnectionType.ToString();
            SignalBarsText.Text = info.SignalStrength.GetValueOrDefault(0).ToString();
            NetworkNamesText.Text = string.Join(", ", info.NetworkNames);

            // 获取当前网络的 IP 地址
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile?.NetworkAdapter != null)
            {
                var hostNames = NetworkInformation.GetHostNames()
                    .Where(hn => hn.IPInformation?.NetworkAdapter?.NetworkAdapterId == profile.NetworkAdapter.NetworkAdapterId);

                // IPv4
                var ipv4s = hostNames
                    .Where(hn => hn.Type == HostNameType.Ipv4)
                    .Select(hn => hn.CanonicalName);

                // IPv6
                var ipv6s = hostNames
                    .Where(hn => hn.Type == HostNameType.Ipv6)
                    .Select(hn => hn.CanonicalName);

                // 可以分开显示
                string ipText = "";
                if (ipv4s.Any())
                    ipText += "IPv4: " + string.Join(", ", ipv4s);
                if (ipv6s.Any())
                    ipText += (ipText.Length > 0 ? "\n" : "") + "IPv6: " + string.Join(", ", ipv6s);

                IpAddressText.Text = ipText;
            }
            else
            {
                IpAddressText.Text = "N/A";
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.Trim();

            HighlightDataGridRow(DgTcpPorts, searchText);
            HighlightDataGridRow(DgUdpPorts, searchText);
        }

        private void HighlightDataGridRow(CommunityToolkit.WinUI.UI.Controls.DataGrid dataGrid, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return;

            dataGrid.SelectedItems.Clear();
            searchText = searchText.Trim();

            bool isNumber = int.TryParse(searchText, out int number);

            if (dataGrid.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is NetworkPortModel port)
                    {
                        bool match = false;

                        if (isNumber)
                        {
                            // 1️ PID
                            if (port.PID == number)
                                match = true;

                            // 2️ 本地端口（TCP / UDP 都有）
                            if (port.LocalPort == number)
                                match = true;

                            // 3️ 远程端口（只对 TCP 有意义）
                            if (!string.IsNullOrEmpty(port.RemoteAddress) &&
                                port.RemotePort == number)
                                match = true;
                        }
                        else
                        {
                            // 4️ 进程名
                            if (!string.IsNullOrEmpty(port.ProcessName) &&
                                port.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                            {
                                match = true;
                            }
                        }

                        if (match)
                            dataGrid.SelectedItems.Add(item);
                    }
                }
            }

            if (dataGrid.SelectedItems.Count > 0)
                dataGrid.ScrollIntoView(dataGrid.SelectedItems[0], null);
        }

    }
}
