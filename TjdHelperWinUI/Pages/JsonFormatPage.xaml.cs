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
using Microsoft.Extensions.DependencyInjection;
using TjdHelperWinUI.ViewModels;
using System.Reflection;
using Windows.UI.WebUI;
using Microsoft.Web.WebView2.Core;
using System.Threading;
using Windows.UI.ViewManagement;
using Windows.UI;
using TjdHelperWinUI.Tools;
using System.Text.Json;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class JsonFormatPage : Page
    {
        public JsonFormatPageViewModel viewModel { get; set; }

        public JsonFormatPage()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                viewModel = App.Services.GetService<JsonFormatPageViewModel>();
                rootElement.DataContext = viewModel;
            }

            LoadMonaco();
        }

        #region 解析消息内容
        /// <summary>
        /// 解析消息内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            //messge
            var monacoPostMsgStr = args.TryGetWebMessageAsString();

            //更改主题
            if (monacoPostMsgStr.Split("->")[0] == "changeTheme" && monacoPostMsgStr.Split("->")[1] == "vs-light")
            {
                ChangeEditorTheme("vs-dark");
            }
            if (monacoPostMsgStr.Split("->")[0] == "changeTheme" && monacoPostMsgStr.Split("->")[1] == "vs-dark")
            {
                ChangeEditorTheme("vs-light");
            }

            //格式化Json
            if (monacoPostMsgStr.StartsWith("checkAndFormatJson->"))
            {
                string content = monacoPostMsgStr.Split("->")[1];
                viewModel.StrJsonPrase = content;
                viewModel.CheckAndFormatJsonCommandExecute(null);
            }

            //压缩Json
            if (monacoPostMsgStr.StartsWith("compressJson->"))
            {
                string content = monacoPostMsgStr.Split("->")[1];
                viewModel.StrJsonPrase = content;
                viewModel.CompresseJsonCommandExecute(null);
                await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setEditorContent('{viewModel.StrJsonPrase}')");
            }

            //转义Json
            if (monacoPostMsgStr.StartsWith("serializeJson->"))
            {
                string content = monacoPostMsgStr.Split("->")[1];
                viewModel.StrJsonPrase = content;
                viewModel.SerializeJsonCommandExecute(null);
                string escapedJson = JsonSerializer.Serialize(viewModel.StrJsonPrase);
                string trimmed = escapedJson.Substring(1, escapedJson.Length - 2); // 去掉前后引号
                string script = $"setEditorContent('{trimmed}')";
                await MonacoWebView.CoreWebView2.ExecuteScriptAsync(script);
            }

            //去转义Json
            if (monacoPostMsgStr.StartsWith("deSerializeJson->"))
            {
                string content = monacoPostMsgStr.Split("->")[1];
                viewModel.StrJsonPrase = content;
                viewModel.DeserializeJsonCommandExecute(null);
                string script = $"setEditorContent('{viewModel.StrJsonPrase}')";
                await MonacoWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
        #endregion

        #region 加载 Monaco 编辑器
        /// <summary>
        /// 加载 Monaco 编辑器
        /// </summary>
        private async void LoadMonaco()
        {
            await MonacoWebView.EnsureCoreWebView2Async(); // 等待 WebView2 核心初始化
            // 监听 WebView2 的加载完成事件
            MonacoWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            string appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string htmlPath = Path.Combine(appFolder, "Resources", "monaco.html");
            if (File.Exists(htmlPath))
            {
                // 封送到 UI 线程
                MonacoWebView.Source = new Uri($"file:///{htmlPath.Replace("\\", "/")}");

            }

            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                Thread.Sleep(500);
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (SystemThemeHelper.IsSystemDarkTheme())
                    {
                        ChangeEditorTheme("vs-dark");
                    }
                    else
                    {
                        ChangeEditorTheme("vs-light");
                    }

                    LoadLanguage();
                });
            }));
        }
        #endregion

        #region 加载代码语言
        /// <summary>
        /// 加载代码语言
        /// </summary>
        private async void LoadLanguage()
        {
            if (MonacoWebView.CoreWebView2 != null)
            {
                string script = $"setEditorLanguage('json');";
                await MonacoWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
        #endregion

        #region 加载默认编辑内容
        /// <summary>
        /// 加载默认编辑内容
        /// </summary>
        private async void LoadDefaultEditContent()
        {
            // JavaScript 代码，调用 setEditorContent() 方法
            string script = $"setEditorContent('//在这里输入Json');";

            // 让 WebView2 执行 JavaScript
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
        #endregion

        #region monaco自适应窗体大小
        /// <summary>
        /// monaco自适应窗体大小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void JsonFormatPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MonacoWebView.Width = this.ActualWidth;
            MonacoWebView.Height = this.ActualHeight;
        }
        #endregion

        #region 更改编辑器主题
        /// <summary>
        /// 更改编辑器主题
        /// </summary>
        /// <param name="theme"></param>
        private async void ChangeEditorTheme(string theme)
        {
            if (MonacoWebView.CoreWebView2 != null)
            {
                string script = $"setEditorTheme('{theme}');";
                string result = await MonacoWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
        #endregion

        #region 格式化校验Json
        /// <summary>
        /// 格式化校验Json
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnCheckAndFormatClicked(object sender, RoutedEventArgs e)
        {
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync("formatCode()");
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage('checkAndFormatJson->'+editor.getValue())");
        }
        #endregion

        #region 改变亮暗主题
        /// <summary>
        /// 改变亮暗主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChangeThemeClicked(object sender, RoutedEventArgs e)
        {
            await MonacoWebView.ExecuteScriptAsync("window.chrome.webview.postMessage('changeTheme->'+getEditorTheme());");
        }

        #endregion

        #region 压缩Json
        /// <summary>
        /// 压缩Json
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnCompresseJsonClicked(object sender, RoutedEventArgs e)
        {
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage('compressJson->'+editor.getValue())");
        }
        #endregion

        #region 序列化Json
        /// <summary>
        /// 序列化Json
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSerializeJsonClicked(object sender, RoutedEventArgs e)
        {
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage('serializeJson->'+editor.getValue())");
        }
        #endregion

        #region 反序列化Json
        /// <summary>
        /// 反序列化Json
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnDeserializeJsonClicked(object sender, RoutedEventArgs e)
        {
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync("window.chrome.webview.postMessage('deSerializeJson->'+editor.getValue())");
        }
        #endregion

        #region 清空Json
        /// <summary>
        /// 清空Json
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnClearJsonClicked(object sender, RoutedEventArgs e)
        {
            await MonacoWebView.CoreWebView2.ExecuteScriptAsync("setEditorContent('')");
            viewModel.StrJsonPrase = "";
            viewModel.IsMonacoShown = true;
            viewModel.IsTreeViewShown = false;
        }
        #endregion
    }
}
