using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;
using Ionic.Zip;

namespace TjdHelperWinUI.ViewModels
{
    public class FileHelperPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 属性改变通知事件触发方法
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 命令定义
        public ICommand ChooseSplitFileCommand { get; }
        public ICommand ChooseUnzipFileCommand { get; }
        public ICommand StartFileSplitCommand { get; set; }
        public ICommand OpenSplitFilePathCommand { get; set; }
        public ICommand StartUnzipCommand { get; set; }
        public ICommand OpenUnzipFilePathCommand { get; }

        #region 分片相关属性

        /// <summary>
        /// 待分片的文件完整路径
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
        /// 分片大小，单位MB，文本框绑定的字符串形式
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
        /// 分片日志内容，用于显示分片后的文件信息和校验码
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
        /// 分片操作进度条是否显示
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
        /// 分片操作进度条当前值（0-100）
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

        /// <summary>
        /// 分片输出文件夹完整路径
        /// </summary>
        public string SplitFileSavePath { get; set; }

        #endregion

        #region 解压相关属性

        /// <summary>
        /// 待解压的文件完整路径
        /// </summary>
        private string _toUnzipFilePath;
        public string ToUnzipFilePath
        {
            get => _toUnzipFilePath;
            set
            {
                if (_toUnzipFilePath != value)
                {
                    _toUnzipFilePath = value;
                    OnPropertyChanged(nameof(ToUnzipFilePath));
                }
            }
        }

        /// <summary>
        /// 解压密码，如果没有密码则为空字符串
        /// </summary>
        private string _unzipPwd;
        public string UnzipPwd
        {
            get => _unzipPwd;
            set
            {
                if (_unzipPwd != value)
                {
                    _unzipPwd = value;
                    OnPropertyChanged(nameof(UnzipPwd));
                }
            }
        }

        /// <summary>
        /// 当前已完成解压的文件数，辅助进度计算
        /// </summary>
        private int _filesExtractedSoFar = 0;

        /// <summary>
        /// 当前正在解压的文件名，实时显示在界面
        /// </summary>
        private string _currentUnzipFileName;
        public string CurrentUnzipFileName
        {
            get => _currentUnzipFileName;
            set
            {
                if (_currentUnzipFileName != value)
                {
                    _currentUnzipFileName = value;
                    OnPropertyChanged(nameof(CurrentUnzipFileName));
                }
            }
        }

        /// <summary>
        /// 解压操作进度条是否显示
        /// </summary>
        private Visibility _unzipProgressVisibility;
        public Visibility UnzipProgressVisibility
        {
            get => _unzipProgressVisibility;
            set
            {
                if (_unzipProgressVisibility != value)
                {
                    _unzipProgressVisibility = value;
                    OnPropertyChanged(nameof(UnzipProgressVisibility));
                }
            }
        }

        /// <summary>
        /// 解压操作进度条当前值（0-100）
        /// </summary>
        private int _unzipProgressValue;
        public int UnzipProgressValue
        {
            get => _unzipProgressValue;
            set
            {
                if (_unzipProgressValue != value)
                {
                    _unzipProgressValue = value;
                    OnPropertyChanged(nameof(UnzipProgressValue));
                }
            }
        }

        /// <summary>
        /// 解压输出文件夹完整路径
        /// </summary>
        public string UnzipFilePath { get; set; }

        #endregion

        /// <summary>
        /// 构造函数，初始化命令和目录
        /// </summary>
        public FileHelperPageViewModel()
        {
            // 初始化进度条隐藏和数值归零
            SplitProgressVisibility = Visibility.Collapsed;
            SplitProgressValue = 0;
            UnzipProgressVisibility = Visibility.Collapsed;
            UnzipProgressValue = 0;

            // 确保分片和解压目录存在
            SplitFileSavePath = EnsureDirectory("Split");
            UnzipFilePath = EnsureDirectory("Unzip");

            // 命令绑定
            ChooseSplitFileCommand = new RelayCommand(async _ =>
            {
                await ChooseFilePathAsync(path => ToSplitFilePath = path);
            });

            ChooseUnzipFileCommand = new RelayCommand(async _ =>
            {
                await ChooseFilePathAsync(path => ToUnzipFilePath = path);
            });

            StartFileSplitCommand = new RelayCommand(StartFileSplitCommandExecute);
            StartUnzipCommand = new RelayCommand(StartUnzipCommandExecute);

            OpenSplitFilePathCommand = new RelayCommand(_ =>
            {
                string unzipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Split");
                OpenFolder(unzipPath);
            });

            OpenUnzipFilePathCommand = new RelayCommand(_ =>
            {
                string unzipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Unzip");
                OpenFolder(unzipPath);
            });
        }

        /// <summary>
        /// 确保指定名称的目录存在，不存在则创建
        /// </summary>
        /// <param name="folderName">目录名（相对于应用基目录）</param>
        /// <returns>目录完整路径</returns>
        private static string EnsureDirectory(string folderName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
            Directory.CreateDirectory(path); // 不存在时创建，存在则无操作
            return path;
        }

        /// <summary>
        /// 弹出文件选择对话框，选择单个文件路径，结果通过回调返回
        /// </summary>
        /// <param name="setPathAction">回调，用于接收选择的文件路径</param>
        private async Task ChooseFilePathAsync(Action<string> setPathAction)
        {
            string? selectedPath = await FilePickerHelper.PickSingleFilePathAsync(App.MainWindow);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                setPathAction(selectedPath);
            }
            else
            {
                NotificationHelper.Show("通知", "操作已取消");
            }
        }

        /// <summary>
        /// 开始执行文件分片操作，异步处理并更新界面进度和日志
        /// </summary>
        /// <param name="obj">命令参数，未使用</param>
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
                    // 调用文件分片工具，返回分片后的文件列表
                    var partFiles = FileSplitter.CreatePhysicalChunks(
                        ToSplitFilePath,
                        splitSizeBytes,
                        outputPath,
                        progress =>
                        {
                            // UI线程更新分片进度条
                            dispatcher.TryEnqueue(() =>
                            {
                                SplitProgressValue = progress;
                            });
                        });

                    // 计算每个分片文件的 Adler-32 校验码并构建日志内容
                    var sb = new StringBuilder();
                    sb.AppendLine($"共分片 {partFiles.Count} 个文件：");
                    foreach (var file in partFiles)
                    {
                        uint checksum = Adler32Helper.ComputeChecksum(file);
                        sb.AppendLine($"{Path.GetFileName(file)} - Adler32: {checksum}");
                    }

                    // UI线程更新日志显示及进度条置为100%
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
                // 分片结束，隐藏进度条，重置进度值
                SplitProgressVisibility = Visibility.Collapsed;
                SplitProgressValue = 0;
            }
        }

        /// <summary>
        /// 开始执行解压操作，异步处理并实时更新当前解压文件名和进度条
        /// </summary>
        /// <param name="obj">命令参数，未使用</param>
        public void StartUnzipCommandExecute(object obj)
        {
            if (string.IsNullOrEmpty(ToUnzipFilePath))
            {
                NotificationHelper.Show("通知", "请选择要解压的文件");
                return;
            }

            try
            {
                // 显示进度条，重置进度和文件名
                UnzipProgressVisibility = Visibility.Visible;
                UnzipProgressValue = 0;
                CurrentUnzipFileName = string.Empty;
                _filesExtractedSoFar = 0;

                var dispatcher = App.MainWindow.DispatcherQueue;

                Task.Run(() =>
                {
                    var options = new ReadOptions
                    {
                        Encoding = Encoding.UTF8
                    };

                    using (var zip = Ionic.Zip.ZipFile.Read(ToUnzipFilePath, options))
                    {
                        if (!string.IsNullOrEmpty(UnzipPwd))
                        {
                            zip.Password = UnzipPwd;
                        }

                        // 预先获取压缩包中总文件数，用于计算进度
                        int totalEntries = zip.Entries.Count;

                        // 订阅解压进度事件，实时更新当前文件和整体进度
                        zip.ExtractProgress += (s, e) =>
                        {
                            if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
                            {
                                dispatcher.TryEnqueue(() =>
                                {
                                    CurrentUnzipFileName = e.CurrentEntry.FileName;
                                    _filesExtractedSoFar = e.EntriesExtracted;
                                });
                            }
                            else if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
                            {
                                if (totalEntries > 0 && e.TotalBytesToTransfer > 0)
                                {
                                    double fileProgress = (double)e.BytesTransferred / e.TotalBytesToTransfer;
                                    double overallProgress = ((_filesExtractedSoFar + fileProgress) / totalEntries) * 100;

                                    dispatcher.TryEnqueue(() =>
                                    {
                                        UnzipProgressValue = (int)Math.Round(overallProgress);
                                    });
                                }
                            }
                        };

                        // 执行解压，遇到同名文件覆盖
                        try
                        {
                            zip.ExtractAll(UnzipFilePath, ExtractExistingFileAction.OverwriteSilently);

                            // 解压完成后通知和UI重置
                            dispatcher.TryEnqueue(() =>
                            {
                                NotificationHelper.Show("通知", "解压成功");
                                Process.Start("explorer.exe", UnzipFilePath);
                                ResetUnzipUI();
                            });
                        }
                        catch (Exception ex)
                        {
                            dispatcher.TryEnqueue(() =>
                            {
                                ResetUnzipUI();
                                NotificationHelper.Show("错误", ex.ToString());
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("通知", $"解压失败: {ex.Message}");
                UnzipProgressVisibility = Visibility.Collapsed;
                UnzipProgressValue = 0;
                CurrentUnzipFileName = string.Empty;
            }
        }

        // 私有方法：重置解压相关UI状态
        private void ResetUnzipUI()
        {
            UnzipProgressVisibility = Visibility.Collapsed;
            UnzipProgressValue = 0;
            CurrentUnzipFileName = string.Empty;
        }

        /// <summary>
        /// 通过系统文件资源管理器打开指定目录
        /// </summary>
        /// <param name="folderPath">文件夹完整路径</param>
        private void OpenFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Process.Start("explorer.exe", folderPath);
            }
            else
            {
                NotificationHelper.Show("错误", $"文件夹不存在: {folderPath}");
            }
        }
    }
}
