using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class DeepSeekPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private CancellationTokenSource _cts;
        private readonly StringBuilder _sbMarkdown = new StringBuilder();

        private bool _isStreaming;
        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                if (_isStreaming != value)
                {
                    _isStreaming = value;
                    OnPropertyChanged(nameof(IsStreaming));
                    SendButtonGlyph = _isStreaming ? "\uEE95" : "\uF0AD";
                }
            }
        }

        private string _sendButtonGlyph = "\uF0AD"; // 初始箭头
        public string SendButtonGlyph
        {
            get => _sendButtonGlyph;
            set
            {
                if (_sendButtonGlyph != value)
                {
                    _sendButtonGlyph = value;
                    OnPropertyChanged(nameof(SendButtonGlyph));
                }
            }
        }

        private string _strQuery;
        public string StrQuery
        {
            get => _strQuery;
            set
            {
                if (_strQuery != value)
                {
                    _strQuery = value;
                    OnPropertyChanged(nameof(StrQuery));
                }
            }
        }

        private string _strShowFinMarkdown;
        public string StrShowFinMarkdown
        {
            get => _strShowFinMarkdown;
            set
            {
                if (_strShowFinMarkdown != value)
                {
                    _strShowFinMarkdown = value;
                    OnPropertyChanged(nameof(StrShowFinMarkdown));
                }
            }
        }

        public ICommand ChatCompletionCommand { get; }
        public ICommand ClearChatCommand { get; }

        private readonly DeepSeekChatHelper _deepSeek;

        // 增量事件，View 层订阅用于流式追加
        public event Action<string>? OnNewDelta;

        public DeepSeekPageViewModel()
        {
            var deepSeekAPIKey = SettingsHelper.GetSetting<string>("DeepSeekAPIKey");
            _deepSeek = new DeepSeekChatHelper(deepSeekAPIKey);

            ChatCompletionCommand = new RelayCommand(ChatCompletionCommandExecute);
            ClearChatCommand = new RelayCommand(ClearChatCommandExecute);
        }

        private async void ChatCompletionCommandExecute(object obj)
        {
            if (IsStreaming)
            {
                _cts?.Cancel();
                IsStreaming = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(StrQuery)) return;

            var userInput = StrQuery;
            StrQuery = "";

            _sbMarkdown.AppendLine("🧑 你：");
            _sbMarkdown.AppendLine(userInput);
            _sbMarkdown.AppendLine();

            // ⬇ 用户输入后换两行
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                OnNewDelta?.Invoke($"\r\n🧑 你：{userInput}\r\n\r\n");
            });

            _deepSeek.AddUserMessage(userInput);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsStreaming = true;

            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        _sbMarkdown.AppendLine("🤖 DeepSeek：");
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            OnNewDelta?.Invoke("🤖 DeepSeek：");
                        });

                        await _deepSeek.StreamReplyAsync(delta =>
                        {
                            if (token.IsCancellationRequested) return;

                            var formattedDelta = delta.Replace("\n", "\r\n");

                            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                            {
                                OnNewDelta?.Invoke(formattedDelta);
                            });

                            _sbMarkdown.Append(formattedDelta);
                        });

                        // ⬇ DeepSeek 输出结束后追加换行 + 分割线
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                            {
                                OnNewDelta?.Invoke($"\r\n\r\n-------------------------------------------------------------------------------------------\r\n\r\n");

                                // ⬇ 每轮结束写入Markdown最终内容（多轮累积）
                                _sbMarkdown.AppendLine();
                                _sbMarkdown.AppendLine("\r\n---\r\n");  // 分割每条回复
                                _sbMarkdown.AppendLine();

                                StrShowFinMarkdown = _sbMarkdown.ToString(); // <-- 🔥 多轮越来越长
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            OnNewDelta?.Invoke($"❗请求失败：{ex.Message}\r\n");
                        });
                    }
                }, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsStreaming = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void ClearChatCommandExecute(object obj)
        {
            StrQuery = "";
            _sbMarkdown.Clear();
            OnNewDelta?.Invoke(""); // 清空 UI

            _deepSeek.ResetSession();
        }
    }
}