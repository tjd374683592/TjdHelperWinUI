using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;
using Windows.ApplicationModel.DataTransfer;
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
                    .Select(hn => hn.CanonicalName)
                    .Distinct()
                    .ToList();

                // IPv6
                var ipv6s = hostNames
                    .Where(hn => hn.Type == HostNameType.Ipv6)
                    .Select(hn => hn.CanonicalName)
                    .Distinct()
                    .ToList();

                RenderIpAddresses(ipv4s, ipv6s);
            }
            else
            {
                RenderIpAddresses([], []);
            }
        }

        private void RenderIpAddresses(IReadOnlyList<string> ipv4s, IReadOnlyList<string> ipv6s)
        {
            IpAddressPanel.Children.Clear();

            if (ipv4s.Count == 0 && ipv6s.Count == 0)
            {
                AddInlineText("N/A", isSecondary: false);
                return;
            }

            bool hasPreviousSection = false;

            if (ipv4s.Count > 0)
            {
                AddIpSection("IPv4", ipv4s, hasPreviousSection);
                hasPreviousSection = true;
            }

            if (ipv6s.Count > 0)
            {
                AddIpSection("IPv6", ipv6s, hasPreviousSection);
            }
        }

        private void AddIpSection(string label, IReadOnlyList<string> addresses, bool hasPreviousSection)
        {
            if (addresses.Count == 0)
            {
                return;
            }

            if (hasPreviousSection)
            {
                IpAddressPanel.Children.Add(new Border { Width = 20 });
            }

            IpAddressPanel.Children.Add(new TextBlock
            {
                Text = $"{label}: ",
                FontSize = 16,
                TextWrapping = TextWrapping.NoWrap
            });

            for (int i = 0; i < addresses.Count; i++)
            {
                string address = addresses[i];
                var addressTextBlock = new TextBlock
                {
                    Text = address,
                    Tag = address,
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 0),
                    TextWrapping = TextWrapping.NoWrap
                };

                addressTextBlock.Tapped += IpAddressItem_Tapped;
                IpAddressPanel.Children.Add(addressTextBlock);

                if (i < addresses.Count - 1)
                {
                    AddInlineText(", ", isSecondary: true);
                }
            }
        }

        private void AddInlineText(string text, bool isSecondary = true)
        {
            IpAddressPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 16,
                TextWrapping = TextWrapping.NoWrap,
                Foreground = isSecondary
                    ? IpAddressLabelText.Foreground
                    : NetworkNamesText.Foreground
            });
        }

        private Brush GetIpVersionLabelBrush()
        {
            if (ActualTheme == ElementTheme.Light)
            {
                return new SolidColorBrush(Colors.Black);
            }

            return IpAddressLabelText.Foreground;
        }

        private void IpAddressItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is not TextBlock textBlock || textBlock.Tag is not string ipAddress || string.IsNullOrWhiteSpace(ipAddress))
            {
                NotificationHelper.Show("复制失败", "当前没有可复制的 IP 地址。");
                return;
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(ipAddress);
            Clipboard.SetContent(dataPackage);

            NotificationHelper.Show("IP 已复制", $"已经复制了 {ipAddress}");
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
