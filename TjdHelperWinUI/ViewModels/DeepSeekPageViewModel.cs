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
                        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            OnNewDelta?.Invoke("🤖 DeepSeek：");
                        });

                        _sbMarkdown.Clear();

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
                            // ⬇ DeepSeek 输出结束后追加换行 + 分割线 + 再换行保证下一次输入不会贴在后面
                            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                            {
                                OnNewDelta?.Invoke($"\r\n\r\n-------------------------------------------------------------------------------------------\r\n\r\n");
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