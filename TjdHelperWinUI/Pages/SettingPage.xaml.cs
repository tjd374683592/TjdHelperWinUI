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

            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.ContainsKey("PaneDisplayMode"))
            {
                var paneDisplayMode = settings.Values["PaneDisplayMode"].ToString();
                if (paneDisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Left.ToString())
                {
                    navigationLocation.SelectedIndex = 0;
                }
                else if (paneDisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top.ToString())
                {
                    navigationLocation.SelectedIndex = 1;
                }
            }
            else
            {
                navigationLocation.SelectedIndex = 0; // Default to Left
            }

            _isInitialized = true; // ✅ 设定初始化完成
        }

        private void navigationLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return; // ✅ 防止初始化阶段触发逻辑

            if (navigationLocation.SelectedIndex == 0)
            {
                App.MainWindow.MainNavigationView.PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Left;
            }
            else if (navigationLocation.SelectedIndex == 1)
            {
                App.MainWindow.MainNavigationView.PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top;
            }

            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["PaneDisplayMode"] = App.MainWindow.MainNavigationView.PaneDisplayMode.ToString();
        }
    }
}