using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Pages;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels; // 需要 WinRT 扩展支持 COM 接口转换
using Windows.Graphics;
using Windows.Storage;
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
        // MainWindow.xaml.cs
        public NavigationView MainNavigationView { get; private set; }  // 对外暴露供其他页面访问
        public Frame MainWindowNavigationFrame { get; set; }
        private bool _hasAppliedStartupLocalization;
        private Page _currentLocalizedPage;
        public MainWindow()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                rootElement.DataContext = App.Services.GetService<MainWindowViewModel>();
                rootElement.ActualThemeChanged += RootElement_ActualThemeChanged;
            }

            // 获取当前窗口对应的 AppWindow 对象
            var appWindow = GetAppWindowForCurrentWindow();

            // 启用自定义标题栏，让 XAML 中的 AppTitleBar 延伸到系统标题栏区域
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            // 监听窗口变化事件，用于处理最大化、最小化和 DPI 变化等场景
            appWindow.Changed += (s, e) =>
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

                // 获取 DPI 缩放
                double dpiScale = DpiHelper.GetWindowDpiScale(hwnd);

                // 将 RightInset 转换为逻辑像素
                double rightInsetDIP = appWindow.TitleBar.RightInset / dpiScale;
                if (rightInsetDIP < 0)
                    rightInsetDIP = 138;

                AppTitleBar.Padding = new Thickness(0, 0, rightInsetDIP, 0);
            };


            // 让 WinUI 使用 AppTitleBar 作为窗口拖拽区域
            SetTitleBar(AppTitleBar);

            // 设置 Mica 背景
            TrySetMicaBackground();

            ApplyAppTheme(LoadAppTheme());

            SetWindowSizeAndCenter(1450, 900); // 设置窗口大小并居中

            MainNavigationView = this.MainNavigation;
            MainNavigationView.PaneDisplayMode = LoadPaneDisplayMode();

            MainWindowNavigationFrame = this.MainFrame;
            MainFrame.Navigated += MainFrame_Navigated;
            Activated += MainWindow_Activated;
        }

        /// <summary>
        /// 获取当前 WinUI 3 窗口对应的 AppWindow 对象
        /// </summary>
        private AppWindow GetAppWindowForCurrentWindow()
        {
            // 先获取窗口句柄 HWND
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // 根据 HWND 获取 WindowId
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);

            // 根据 WindowId 获取 AppWindow 对象
            return AppWindow.GetFromWindowId(windowId);
        }

        #region 设置 Mica 效果
        private MicaController micaController;
        private SystemBackdropConfiguration backdropConfig;

        private void TrySetMicaBackground()
        {
            // 获取窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // 自定义标题栏设置，确保 Mica 效果能正确显示
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }

            // 仅在支持 Mica 的系统上启用
            if (MicaController.IsSupported())
            {
                backdropConfig = new SystemBackdropConfiguration
                {
                    IsInputActive = true,
                    Theme = SystemBackdropTheme.Default
                };

                micaController = new MicaController();
                micaController.SetSystemBackdropConfiguration(backdropConfig);

                // 将当前窗口注册为系统背景目标
                micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            }
        }
        #endregion

        #region 窗口关闭时释放 Mica 资源
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // 确保 Mica 或 Acrylic 控制器被正确释放
            if (micaController != null)
            {
                micaController.Dispose();
                micaController = null;
            }
        }
        #endregion

        #region 设置窗口位置和默认大小
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

                    // 移动窗口到屏幕中央
                    appWindow.Move(new PointInt32(centerX, centerY));
                }
            }
        }
        #endregion

        public void ApplyAppTheme(ElementTheme theme)
        {
            if (Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                ApplyTitleBarTheme(rootElement.ActualTheme);
            }
        }

        public ElementTheme GetCurrentAppTheme()
        {
            if (Content is FrameworkElement rootElement)
            {
                return rootElement.RequestedTheme;
            }

            return ElementTheme.Default;
        }

        private ElementTheme LoadAppTheme()
        {
            var themeString = SettingsHelper.GetSetting("AppTheme", "Default");
            return Enum.TryParse(themeString, out ElementTheme theme) ? theme : ElementTheme.Default;
        }

        private void RootElement_ActualThemeChanged(FrameworkElement sender, object args)
        {
            ApplyTitleBarTheme(sender.ActualTheme);
        }

        private void ApplyTitleBarTheme(ElementTheme actualTheme)
        {
            var appWindow = GetAppWindowForCurrentWindow();
            var titleBar = appWindow.TitleBar;
            bool isDarkTheme = actualTheme == ElementTheme.Dark;

            titleBar.ButtonForegroundColor = isDarkTheme ? Colors.White : Colors.Black;
            titleBar.ButtonHoverForegroundColor = isDarkTheme ? Colors.White : Colors.Black;
            titleBar.ButtonPressedForegroundColor = isDarkTheme ? Colors.White : Colors.Black;
            titleBar.ButtonInactiveForegroundColor = isDarkTheme ? Colors.LightGray : Colors.DimGray;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = isDarkTheme
                ? Windows.UI.Color.FromArgb(32, 255, 255, 255)
                : Windows.UI.Color.FromArgb(20, 0, 0, 0);
            titleBar.ButtonPressedBackgroundColor = isDarkTheme
                ? Windows.UI.Color.FromArgb(64, 255, 255, 255)
                : Windows.UI.Color.FromArgb(32, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            if (backdropConfig != null)
            {
                backdropConfig.Theme = actualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                };
            }
        }

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
                // 用户直接按下回车
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
        /// 导航加载完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            // 默认导航到首页
            MainFrame.Navigate(typeof(HomePage));
            DispatcherQueue.TryEnqueue(LocalizationService.RefreshApplicationLanguage);
        }

        public void RefreshLocalization()
        {
            LocalizationService.ApplyToObject(MainNavigation);
            LocalizationService.ApplyToObject(TestButton1TeachingTip);
            LocalizationService.ApplyToWindow(this);

            if (MainFrame.Content is Page currentPage)
            {
                LocalizationService.ApplyToObject(currentPage);
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= MainWindow_Activated;
            QueueLocalizationRefresh();
        }

        private void QueueLocalizationRefresh()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                RefreshLocalization();
                MainNavigation.UpdateLayout();
                DispatcherQueue.TryEnqueue(RefreshLocalization);
            });
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (_currentLocalizedPage != null)
            {
                _currentLocalizedPage.Loaded -= CurrentPage_Loaded;
            }

            if (MainFrame.Content is Page currentPage)
            {
                _currentLocalizedPage = currentPage;
                currentPage.Loaded -= CurrentPage_Loaded;
                currentPage.Loaded += CurrentPage_Loaded;
                LocalizationService.ApplyToObject(currentPage);
            }

            if (!_hasAppliedStartupLocalization)
            {
                _hasAppliedStartupLocalization = true;
                DispatcherQueue.TryEnqueue(LocalizationService.RefreshApplicationLanguage);
            }
        }

        private void CurrentPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Page currentPage)
            {
                return;
            }

            currentPage.Loaded -= CurrentPage_Loaded;

            DispatcherQueue.TryEnqueue(() =>
            {
                LocalizationService.ApplyToObject(currentPage);
                currentPage.UpdateLayout();
                DispatcherQueue.TryEnqueue(() => LocalizationService.ApplyToObject(currentPage));
            });
        }

        /// <summary>
        /// 处理 NavigationView 菜单项点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MainNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag != null)
            {
                // 根据 Tag 导航到对应页面
                var pageType = Type.GetType($"TjdHelperWinUI.Pages.{item.Tag}");
                if (item.Tag.ToString() == "Settings")
                {
                    pageType = Type.GetType("TjdHelperWinUI.Pages.SettingPage");
                }
                if (pageType != null) { MainFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight }); }
            }
        }

        /// <summary>
        /// 返回导航
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
                // 根据用户选择的搜索项导航到对应页面
                MainFrame.Navigate(((PageInfo)args.SelectedItem).PageType);
            }
        }

        // 读取导航栏布局模式
        NavigationViewPaneDisplayMode LoadPaneDisplayMode()
        {
            var modeString = SettingsHelper.GetSetting<string>("PaneDisplayMode", "Left");
            if (Enum.TryParse(modeString, out NavigationViewPaneDisplayMode mode))
            {
                return mode;
            }
            return NavigationViewPaneDisplayMode.Left; // 默认值
        }

        private void PersonPictureCtrl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            TestButton1TeachingTip.IsOpen = true;
        }
    }
}
