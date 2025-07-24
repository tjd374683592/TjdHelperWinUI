using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class FileHelperPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand ChooseFilePathCommand { get; set; }
        public ICommand StartFileSplitCommand { get; set; }
        public ICommand OpenSplitFilePathCommand { get; set; }

        /// <summary>
        /// 分片文件路径
        /// </summary>
        private string _toSplitFilePath;

        public string ToSplitFilePath
        {
            get { return _toSplitFilePath; }
            set
            {
                if (_toSplitFilePath != value)
                {
                    _toSplitFilePath = value;
                    OnPropertyChanged(nameof(ToSplitFilePath));
                }
            }
        }

        /// <summary>
        /// 分片文件大小
        /// </summary>
        private string _splitFileSize;

        public string SplitFileSize
        {
            get { return _splitFileSize; }
            set
            {
                if (_splitFileSize != value)
                {
                    _splitFileSize = value;
                    OnPropertyChanged(nameof(SplitFileSize));
                }
            }
        }

        /// <summary>
        /// 分片日志
        /// </summary>
        private string _splitDetailsContent;

        public string SplitDetailsContent
        {
            get { return _splitDetailsContent; }
            set
            {
                if (_splitDetailsContent != value)
                {
                    _splitDetailsContent = value;
                    OnPropertyChanged(nameof(SplitDetailsContent));
                }
            }
        }

        /// <summary>
        /// 分片进度条可见性
        /// </summary>
        private Visibility _splitProgressVisibility;

        public Visibility SplitProgressVisibility
        {
            get { return _splitProgressVisibility; }
            set
            {
                if (_splitProgressVisibility != value)
                {
                    _splitProgressVisibility = value;
                    OnPropertyChanged(nameof(SplitProgressVisibility));
                }
            }
        }

        /// <summary>
        /// 分片进度条值
        /// </summary>
        private int _splitProgressValue;

        public int SplitProgressValue
        {
            get { return _splitProgressValue; }
            set
            {
                if (_splitProgressValue != value)
                {
                    _splitProgressValue = value;
                    OnPropertyChanged(nameof(SplitProgressValue));
                }
            }
        }

        public string SplitFileSavePath { get; set; }

        public FileHelperPageViewModel()
        {
            SplitProgressVisibility = Visibility.Collapsed;
            SplitProgressValue = 0;

            // 构建文件夹的完整路径
            SplitFileSavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Split");
            // 判断文件夹是否存在
            if (!Directory.Exists(SplitFileSavePath))
            {
                // 如果不存在，则创建文件夹
                Directory.CreateDirectory(SplitFileSavePath);
            }

            ChooseFilePathCommand = new RelayCommand(ChooseFilePathCommandExecute);
            StartFileSplitCommand = new RelayCommand(StartFileSplitCommandExecute);
            OpenSplitFilePathCommand = new RelayCommand(OpenSplitFilePathCommandExecute);
        }

        /// <summary>
        /// 选择分片文件
        /// </summary>
        /// <param name="obj"></param>
        private async void ChooseFilePathCommandExecute(object obj)
        {
            string? selectedPath = await FilePickerHelper.PickSingleFilePathAsync(App.MainWindow);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                ToSplitFilePath = selectedPath;
            }
            else
            {
                NotificationHelper.Show("通知", "操作已取消");
            }
        }

        /// <summary>
        /// 开始文件分片命令执行方法
        /// </summary>
        /// <param name="obj"></param>
        private async void StartFileSplitCommandExecute(object obj)
        {
            if (string.IsNullOrEmpty(ToSplitFilePath))
            {
                NotificationHelper.Show("通知", "请选择要分片的文件");
                return;
            }
            if (!int.TryParse(SplitFileSize, out int splitSizeMB) || splitSizeMB <= 0)
            {
                NotificationHelper.Show("通知", "请输入正确的分片大小");
                return;
            }

            SplitProgressVisibility = Visibility.Visible;
            SplitProgressValue = 0;

            try
            {
                var dispatcher = App.MainWindow.DispatcherQueue;
                var outputPath = SplitFileSavePath;
                int splitSizeBytes = splitSizeMB * 1024 * 1024;

                await Task.Run(() =>
                {
                    var partFiles = FileSplitter.CreatePhysicalChunks(
                        ToSplitFilePath,
                        splitSizeBytes,
                        outputPath,
                        progress =>
                        {
                            dispatcher.TryEnqueue(() =>
                            {
                                SplitProgressValue = progress;
                            });
                        });

                    // 计算 Adler-32 并构建输出内容
                    var sb = new StringBuilder();
                    sb.AppendLine($"共分片 {partFiles.Count} 个文件：");
                    foreach (var file in partFiles)
                    {
                        uint checksum = Adler32Helper.ComputeChecksum(file);
                        sb.AppendLine($"{Path.GetFileName(file)} - Adler32: {checksum}");
                    }

                    dispatcher.TryEnqueue(() =>
                    {
                        SplitDetailsContent = sb.ToString();
                        SplitProgressValue = 100;
                    });
                });


                NotificationHelper.Show("通知", "文件分片完成");
                Process.Start("explorer.exe", SplitFileSavePath);
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("错误", $"文件分片失败: {ex.Message}");
            }
            finally
            {
                SplitProgressVisibility = Visibility.Collapsed;
                SplitProgressValue = 0;
            }
        }

        /// <summary>
        /// 打开分片文件夹
        /// </summary>
        /// <param name="obj"></param>
        private void OpenSplitFilePathCommandExecute(object obj)
        {
            Process.Start("explorer.exe", SplitFileSavePath);
        }
    }
}