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
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

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
                Thread.Sleep(2000);
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

        #region MonacoWebView拖拽事件
        /// <summary>
        /// MonacoWebView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MonacoWebView_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    var file = items.FirstOrDefault() as StorageFile;

                    if (file != null)
                    {
                        // 判断文件类型，PDF、图片、视频、压缩包时直接用系统默认方式打开
                        string contentType = file.ContentType.ToLower();

                        if (contentType == "application/pdf" || contentType.StartsWith("image/") || contentType.StartsWith("video/") || contentType.StartsWith("application/x-zip-compressed"))
                        {
                            // 这里用 Launcher 打开文件，系统会选择默认应用打开
                            var success = await Windows.System.Launcher.LaunchFileAsync(file);
                            if (!success)
                            {
                                NotificationHelper.Show("无法打开文件: " + file.Name);
                            }
                            return; // 直接返回，不继续下面读文本流程
                        }

                        // 非 PDF/图片/视频/压缩包，按文本读取显示
                        string text = await FileIO.ReadTextAsync(file);

                        await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setEditorContent({JsonSerializer.Serialize(text)});");

                        string lang = GetLanguageFromExtension(Path.GetExtension(file.Name));
                        await MonacoWebView.CoreWebView2.ExecuteScriptAsync($"setEditorLanguage('{lang}');");
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("文件读取失败: " + ex.Message);
            }
            finally
            {
                e.Handled = true;
            }
        }
        #endregion

        #region 根据文件扩展名获取对应的代码语言
        /// <summary>
        /// 根据文件扩展名获取对应的代码语言
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        private string GetLanguageFromExtension(string ext)
        {
            return ext.ToLower() switch
            {
                // Web & Frontend
                ".html" => "html",
                ".htm" => "html",
                ".css" => "css",
                ".scss" => "scss",
                ".sass" => "scss",
                ".less" => "less",
                ".xml" => "xml",
                ".xhtml" => "xml",
                ".svg" => "xml",

                // JavaScript & TypeScript
                ".js" => "javascript",
                ".jsx" => "javascript",
                ".ts" => "typescript",
                ".tsx" => "typescript",

                // JSON / YAML
                ".json" => "json",
                ".jsonc" => "jsonc",
                ".yaml" => "yaml",
                ".yml" => "yaml",

                // C/C++
                ".c" => "c",
                ".cpp" => "cpp",
                ".cc" => "cpp",
                ".cxx" => "cpp",
                ".h" => "cpp",
                ".hpp" => "cpp",

                // C#
                ".cs" => "csharp",

                // Java
                ".java" => "java",

                // Python
                ".py" => "python",

                // Go
                ".go" => "go",

                // Rust
                ".rs" => "rust",

                // PHP
                ".php" => "php",

                // Ruby
                ".rb" => "ruby",

                // Shell
                ".sh" => "shell",
                ".bash" => "shell",

                // PowerShell
                ".ps1" => "powershell",
                ".psm1" => "powershell",

                // SQL
                ".sql" => "sql",

                // Markdown & 文本
                ".md" => "markdown",
                ".markdown" => "markdown",
                ".txt" => "plaintext",
                ".log" => "plaintext",

                // INI / TOML / Config
                ".ini" => "ini",
                ".toml" => "toml",
                ".env" => "ini",

                // Lua
                ".lua" => "lua",

                // Dart
                ".dart" => "dart",

                // Kotlin
                ".kt" => "kotlin",
                ".kts" => "kotlin",

                // Swift
                ".swift" => "swift",

                // R
                ".r" => "r",

                // Objective-C
                ".m" => "objective-c",
                ".mm" => "objective-cpp",

                // Assembly
                ".asm" => "asm",
                ".s" => "asm",

                // F#
                ".fs" => "fsharp",

                // Docker
                "dockerfile" => "dockerfile",
                ".dockerfile" => "dockerfile",

                // Makefile
                "makefile" => "makefile",
                ".mk" => "makefile",

                // Misc
                ".bat" => "bat",
                ".cmd" => "bat",

                _ => "plaintext"
            };
        }
        #endregion
    }
}
