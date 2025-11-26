using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;
using Windows.UI.Core;

namespace TjdHelperWinUI.Pages
{
    public sealed partial class DeepSeekPage : Page
    {
        // 强类型 ViewModel 属性
        private DeepSeekPageViewModel ViewModel { get; }

        public DeepSeekPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            // 从 DI 容器获取 ViewModel
            ViewModel = App.Services.GetService<DeepSeekPageViewModel>();

            // 设置 DataContext
            this.DataContext = ViewModel;

            // 订阅流式输出增量事件
            ViewModel.OnNewDelta += delta =>
            {
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    if (string.IsNullOrEmpty(delta))
                    {
                        tbDeepSeekResponse.Text = ""; // ⭐ 真正清空输出框
                        return;
                    }

                    tbDeepSeekResponse.Text += delta;

                    tbDeepSeekResponse.SelectionStart = tbDeepSeekResponse.Text.Length;
                    tbDeepSeekResponse.SelectionLength = 0;

                    var scrollViewer = GetScrollViewer(tbDeepSeekResponse);
                    scrollViewer?.ChangeView(null, double.MaxValue, null);
                });
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var deepSeekAPIKey = SettingsHelper.GetSetting<string>("DeepSeekAPIKey");
            if (string.IsNullOrEmpty(deepSeekAPIKey))
            {
                NotificationHelper.Show("注意", "请设置DeepSeek API Key");
                App.MainWindow.MainWindowNavigationFrame.Navigate(typeof(SettingPage));
            }
        }

        /// <summary>
        /// 获取 TextBox 内部的 ScrollViewer
        /// </summary>
        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer sv) return sv;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void OnEnterKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // 只有不按Alt时触发发送
            bool altDown = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu)
                            .HasFlag(CoreVirtualKeyStates.Down);

            if (!altDown) // Enter
            {
                args.Handled = true;  // 阻止 TextBox 换行
                if (ViewModel.ChatCompletionCommand.CanExecute(null))
                    ViewModel.ChatCompletionCommand.Execute(null);
            }
            // Alt + Enter 则直接换行，不处理
        }
    }
}