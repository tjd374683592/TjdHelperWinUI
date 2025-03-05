using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Pages;
using TjdHelperWinUI.ViewModels; // 需要 WinRT 进行 COM 互操作
using Windows.Graphics;
using WinRT;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                rootElement.DataContext = App.Services.GetService<MainWindowViewModel>();
            }

            //设置 Mica 背景
            TrySetMicaBackground();

            SetWindowSizeAndCenter(1500, 900); // 设置窗口大小并居中
        }

        #region 设置Mica效果
        private MicaController micaController;
        private SystemBackdropConfiguration backdropConfig;

        private void TrySetMicaBackground()
        {
            // 获取窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // 自定义标题栏，确保 Mica 效果覆盖整个窗口
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }

            // 仅 Windows 11 支持 Mica
            if (MicaController.IsSupported())
            {
                backdropConfig = new SystemBackdropConfiguration
                {
                    IsInputActive = true,
                    Theme = SystemBackdropTheme.Default
                };

                micaController = new MicaController();
                micaController.SetSystemBackdropConfiguration(backdropConfig);

                // 使用 `this.As<ICompositionSupportsSystemBackdrop>()`
                micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            }
        }
        #endregion

        #region 窗体关闭，disposed Mica控制器
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            if (micaController != null)
            {
                micaController.Dispose();
                micaController = null;
            }
        }
        #endregion

        #region 设置启动位置和窗体默认大小
        private void SetWindowSizeAndCenter(int width, int height)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // 设置窗口大小
                appWindow.Resize(new SizeInt32(width, height));

                // 获取屏幕工作区大小
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    int centerX = (displayArea.WorkArea.Width - width) / 2;
                    int centerY = (displayArea.WorkArea.Height - height) / 2;

                    // 移动窗口到居中位置
                    appWindow.Move(new PointInt32(centerX, centerY));
                }
            }
        }
        #endregion

        private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // 搜索控件
            if (args.ChosenSuggestion != null)
            {
                // 用户选择了建议项
                // args.ChosenSuggestion
            }
            else
            {
                // 用户按下回车键
                // args.QueryText
            }
        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Ctrl + F 快捷键
            controlsSearchBox.Focus(FocusState.Programmatic);
        }

        #region 导航相关
        /// <summary>
        /// 导航加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始导航到首页
            MainFrame.Navigate(typeof(HomePage));
        }

        /// <summary>
        /// 选中了NavigationView具体一项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MainNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag != null)
            {
                // 根据 Tag 导航到对应页面
                var pageType = Type.GetType($"TjdHelperWinUI.Pages.{item.Tag}");
                if (pageType != null) { MainFrame.Navigate(pageType); }
            }
        }

        /// <summary>
        /// 点击回退
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MainNavigation_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }
        #endregion

        private void controlsSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem != null)
            {
                // 根据用户选中项目导航到对应页面
                MainFrame.Navigate(((PageInfo)args.SelectedItem).PageType);
            }
        }
    }
}
