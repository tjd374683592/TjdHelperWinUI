using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using TjdHelperWinUI.Tools;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using System.Xml.Linq;
using ZXing.QrCode.Internal;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private bool _isInitialized = false;
        public SettingPage()
        {
            this.InitializeComponent();

            // 使用 SettingsHelper 读取设置
            var paneDisplayMode = SettingsHelper.GetSetting<string>("PaneDisplayMode", "Left");

            if (paneDisplayMode == NavigationViewPaneDisplayMode.Left.ToString())
            {
                navigationLocation.SelectedIndex = 0;
            }
            else if (paneDisplayMode == NavigationViewPaneDisplayMode.Top.ToString())
            {
                navigationLocation.SelectedIndex = 1;
            }
            else
            {
                navigationLocation.SelectedIndex = 0; // Default to Left
            }

            var postmanUrl = SettingsHelper.GetSetting<string>("PostmanProjectUrl");
            if (!string.IsNullOrEmpty(postmanUrl))
            {
                txtPostmanProjectUrl.Text = postmanUrl;
            }

            // ⭐ 自动选中系统主题
            bool isDark = SystemThemeHelper.IsSystemDarkTheme();
            cmbWindowsTheme.SelectedIndex = isDark ? 0 : 1; // 0 = Dark, 1 = Light

            _isInitialized = true;
        }

        private void navigationLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (navigationLocation.SelectedIndex == 0)
            {
                App.MainWindow.MainNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            }
            else if (navigationLocation.SelectedIndex == 1)
            {
                App.MainWindow.MainNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
            }

            // 使用 SettingsHelper 保存设置
            SettingsHelper.SetSetting("PaneDisplayMode", App.MainWindow.MainNavigationView.PaneDisplayMode.ToString());
        }

        private void cmbWindowsTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            var selectedItem = (cmbWindowsTheme.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedItem == "Dark")
            {
                SystemThemeHelper.SetSystemTheme(true);
                SettingsHelper.SetSetting("WindowsTheme", "Dark");
            }
            else if (selectedItem == "Light")
            {
                SystemThemeHelper.SetSystemTheme(false);
                SettingsHelper.SetSetting("WindowsTheme", "Light");
            }
        }

        private void txtPostmanProjectUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            SettingsHelper.SetSetting("PostmanProjectUrl", txtPostmanProjectUrl.Text);
            NotificationHelper.Show("注意", "Postman Project Url更新成功");
        }
    }
}