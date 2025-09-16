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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Pages;
using TjdHelperWinUI.ViewModels; // ��Ҫ WinRT ���� COM ������
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
        public NavigationView MainNavigationView { get; private set; }  // ������ public
        public MainWindow()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // �� DI �����л�ȡ ViewModel
                rootElement.DataContext = App.Services.GetService<MainWindowViewModel>();
            }

            // ��ȡ��ǰ���ڵ� AppWindow ����
            var appWindow = GetAppWindowForCurrentWindow();

            // �����Զ������������ XAML ��� AppTitleBar ������չ��ϵͳ����������
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            // �������ڱ仯�¼���������󻯡���С��DPI �仯�ȣ�
            appWindow.Changed += (s, e) =>
            {
                // AppWindow.TitleBar.RightInset ��ʾ�ұ�ϵͳ��ť����С������󻯡��رգ����ܿ��
                // ���ﶯ̬���� AppTitleBar ���� Padding�����ұ߿ؼ���PersonPicture���ܿ���ť����
                AppTitleBar.Padding = new Thickness(0, 0, appWindow.TitleBar.RightInset, 0);
                if (appWindow.TitleBar.RightInset < 0)
                {
                    AppTitleBar.Padding = new Thickness(0, 0, 138, 0);
                }
            };

            // ���� WinUI ʹ�� AppTitleBar ��Ϊ����ק�ı���������
            SetTitleBar(AppTitleBar);

            //���� Mica ����
            TrySetMicaBackground();

            SetWindowSizeAndCenter(1400, 900); // ���ô��ڴ�С������

            MainNavigationView = this.MainNavigation;
            MainNavigationView.PaneDisplayMode = LoadPaneDisplayMode();

        }

        /// <summary>
        /// ��ȡ��ǰ WinUI 3 ���ڵ� AppWindow ����
        /// </summary>
        private AppWindow GetAppWindowForCurrentWindow()
        {
            // �Ȼ�ȡ���ھ�� HWND
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // ���� HWND ��ȡ WindowId
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);

            // ���� WindowId ��ȡ AppWindow ����
            return AppWindow.GetFromWindowId(windowId);
        }

        #region ����MicaЧ��
        private MicaController micaController;
        private SystemBackdropConfiguration backdropConfig;

        private void TrySetMicaBackground()
        {
            // ��ȡ���ھ��
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // �Զ����������ȷ�� Mica Ч��������������
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }

            // �� Windows 11 ֧�� Mica
            if (MicaController.IsSupported())
            {
                backdropConfig = new SystemBackdropConfiguration
                {
                    IsInputActive = true,
                    Theme = SystemBackdropTheme.Default
                };

                micaController = new MicaController();
                micaController.SetSystemBackdropConfiguration(backdropConfig);

                // ʹ�� `this.As<ICompositionSupportsSystemBackdrop>()`
                micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            }
        }
        #endregion

        #region ����رգ�disposed Mica������
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

        #region ��������λ�úʹ���Ĭ�ϴ�С
        private void SetWindowSizeAndCenter(int width, int height)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // ���ô��ڴ�С
                appWindow.Resize(new SizeInt32(width, height));

                // ��ȡ��Ļ��������С
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    int centerX = (displayArea.WorkArea.Width - width) / 2;
                    int centerY = (displayArea.WorkArea.Height - height) / 2;

                    // �ƶ����ڵ�����λ��
                    appWindow.Move(new PointInt32(centerX, centerY));
                }
            }
        }
        #endregion

        private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // �����ؼ�
            if (args.ChosenSuggestion != null)
            {
                // �û�ѡ���˽�����
                // args.ChosenSuggestion
            }
            else
            {
                // �û����»س���
                // args.QueryText
            }
        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Ctrl + F ��ݼ�
            controlsSearchBox.Focus(FocusState.Programmatic);
        }

        #region �������
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            // ��ʼ��������ҳ
            MainFrame.Navigate(typeof(HomePage));
        }

        /// <summary>
        /// ѡ����NavigationView����һ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MainNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item && item.Tag != null)
            {
                // ���� Tag ��������Ӧҳ��
                var pageType = Type.GetType($"TjdHelperWinUI.Pages.{item.Tag}");
                if (item.Tag.ToString() == "Settings")
                {
                    pageType = Type.GetType("TjdHelperWinUI.Pages.SettingPage");
                }
                if (pageType != null) { MainFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight }); }
            }
        }

        /// <summary>
        /// �������
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
                // �����û�ѡ����Ŀ��������Ӧҳ��
                MainFrame.Navigate(((PageInfo)args.SelectedItem).PageType);
            }
        }

        // ��ȡ����
        NavigationViewPaneDisplayMode LoadPaneDisplayMode()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue("PaneDisplayMode", out object value))
            {
                if (Enum.TryParse(value.ToString(), out NavigationViewPaneDisplayMode mode))
                {
                    return mode;
                }
            }
            return NavigationViewPaneDisplayMode.Left; // Ĭ��ֵ
        }

        private void PersonPictureCtrl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            TestButton1TeachingTip.IsOpen = true;
        }
    }
}
