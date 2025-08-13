using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Extensions.DependencyInjection;
using TjdHelperWinUI.ViewModels;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern nint LoadImage(nint hInst, string lpszName, uint uType, int cx, int cy, uint fuLoad);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern nint SendMessage(nint hWnd, int Msg, nint wParam, nint lParam);

        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x10;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        public static IServiceProvider Services { get; private set; }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<MainWindowViewModel>(); // 让 ViewModel 在整个应用中可用
            services.AddSingleton<EncryptHelperPageViewModel>();
            services.AddSingleton<WinErrorCodeViewModel>();
            services.AddSingleton<AddressHelperPageViewModel>();
            services.AddSingleton<JsonFormatPageViewModel>();
            services.AddScoped<TimeHelperPageViewModel>();
            services.AddScoped<EnDecodePageViewModel>();
            services.AddScoped<QRCodePageViewModel>();
            services.AddScoped<MediaConverterPageViewModel>();
            services.AddScoped<FileHelperPageViewModel>();
            Services = services.BuildServiceProvider();

            m_window = new MainWindow();
            MainWindow = (MainWindow)m_window;//公开给其他页面使用（XamlRoot）

            //设置任务栏icon
            SetWindowIcon(m_window);

            // 扩展内容到标题栏
            m_window.ExtendsContentIntoTitleBar = true;

            m_window.Activate();
        }

        private Window? m_window;

        public static MainWindow MainWindow { get; private set; }

        private void SetWindowIcon(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var hIcon = LoadImage(IntPtr.Zero, "Assets\\logo.ico", IMAGE_ICON, 256, 256, LR_LOADFROMFILE);
            SendMessage(hwnd, WM_SETICON, ICON_BIG, hIcon);  // 任务栏图标
            SendMessage(hwnd, WM_SETICON, ICON_SMALL, hIcon);
        }
    }
}
