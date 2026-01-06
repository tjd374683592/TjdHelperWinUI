using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
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
    }
}
