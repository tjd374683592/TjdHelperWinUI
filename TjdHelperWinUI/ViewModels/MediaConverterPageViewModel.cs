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
    public class MediaConverterPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 待转换音频文件路径
        /// </summary>
        private string _strAudioFilePath;

        public string StrAudioFilePath
        {
            get { return _strAudioFilePath; }
            set
            {
                if (_strAudioFilePath != value)
                {
                    _strAudioFilePath = value;
                    OnPropertyChanged(nameof(StrAudioFilePath));
                }
            }
        }

        /// <summary>
        /// ffmpeg转换日志
        /// </summary>
        private string _strPCMConvertLog;

        public string StrPCMConvertLog
        {
            get { return _strPCMConvertLog; }
            set
            {
                if (_strPCMConvertLog != value)
                {
                    _strPCMConvertLog = value;
                    OnPropertyChanged(nameof(StrPCMConvertLog));
                }
            }
        }

        public ICommand ChooseAudioPathCommand { get; set; }
        public ICommand ConvertToPCMCommand { get; set; }
        public ICommand OpenFilePathCommand { get; set; }
        public string PCMAudioSaveFilePath { get; set; }

        public MediaConverterPageViewModel()
        {
            PCMAudioSaveFilePath = FileHelper.EnsureDirectory("PCMFile");

            ChooseAudioPathCommand = new RelayCommand(async _ =>
            {
                await FileHelper.ChooseFilePathAsync(path => StrAudioFilePath = path);
            });
            ConvertToPCMCommand = new RelayCommand(ConvertToPCMCommandExecute);
            OpenFilePathCommand = new RelayCommand(OpenFilePathCommandExecute);
        }

        private void OpenFilePathCommandExecute(object obj)
        {
            FileHelper.OpenFolder(PCMAudioSaveFilePath);
        }

        private async void ConvertToPCMCommandExecute(object obj)
        {
            if (string.IsNullOrEmpty(StrAudioFilePath) || !File.Exists(StrAudioFilePath))
            {
                NotificationHelper.Show("错误", "请选择有效的音频文件！");
                return;
            }

            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = Path.Combine(currentPath, @"Resources\Tools\ffmpeg\ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                NotificationHelper.Show("错误", $"找不到 ffmpeg.exe，请确认路径是否正确：{ffmpegPath}");
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(StrAudioFilePath);
            string outputPath = Path.Combine(PCMAudioSaveFilePath, fileName + ".pcm");

            StrPCMConvertLog = ""; // 清空日志

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{StrAudioFilePath}\" -f s16le -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) AppendLog(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) AppendLog(e.Data); };

                process.Start();
                // 读取标准输出
                string output = process.StandardOutput.ReadToEnd();
                // 读取错误输出
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();  // 等待进程结束
                int exitCode = process.ExitCode;  // 获取返回的退出代码

                AppendLog("它会将任意格式的音频（比如 .ogx、.mp3、.wav）转换为：原始的、16k 采样率、单声道、16位的小端格式的 PCM 数据");
                AppendLog("");

                if (process.ExitCode == 0)
                {
                    AppendLog("✅ PCM 转换完成！");
                    AppendLog("");
                    AppendLog(error);
                    NotificationHelper.Show("成功", "PCM 转换完成！");
                }
                else
                {
                    AppendLog($"❌ 转换失败，退出码：{process.ExitCode}");
                    AppendLog("");
                    AppendLog(error);
                    NotificationHelper.Show("失败", "PCM 转换失败！");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ 异常：{ex.Message}");
                AppendLog("");
                NotificationHelper.Show("异常", ex.Message);
            }
        }

        private void AppendLog(string text)
        {
            StrPCMConvertLog += text + Environment.NewLine;
        }
    }
}