using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using System.Collections.Generic;

namespace TjdHelperWinUI.Tools
{
    public static class FileHelper
    {
        /// <summary>
        /// 确保指定名称的目录存在，不存在则创建
        /// </summary>
        /// <param name="folderName">目录名（相对于应用基目录）</param>
        /// <returns>目录完整路径</returns>
        public static string EnsureDirectory(string folderName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
            Directory.CreateDirectory(path); // 不存在时创建，存在则无操作
            return path;
        }

        /// <summary>
        /// 弹出文件选择对话框，选择单个文件路径，结果通过回调返回
        /// </summary>
        /// <param name="setPathAction">回调，用于接收选择的文件路径</param>
        public static async Task ChooseFilePathAsync(Action<string> setPathAction)
        {
            string? selectedPath = await PickSingleFilePathAsync(App.MainWindow);

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
        /// 通过系统文件资源管理器打开指定目录
        /// </summary>
        /// <param name="folderPath">文件夹完整路径</param>
        public static void OpenFolder(string folderPath)
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

        public static async Task<string?> PickSingleFilePathAsync(Window window)
        {
            // 创建文件选择器
            var openPicker = new FileOpenPicker();

            // 获取 WinUI 3 窗口的 HWND 句柄
            var hWnd = WindowNative.GetWindowHandle(window);

            // 初始化文件选择器与窗口绑定
            InitializeWithWindow.Initialize(openPicker, hWnd);

            // 设置文件选择器选项
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add("*"); // 所有文件

            // 弹出文件选择器
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                return file.Path;
            }

            return null; // 用户取消选择
        }

        /// <summary>
        /// 将文件分片为内存数据块（流式读取）
        /// </summary>
        /// <param name="filePath">源文件路径</param>
        /// <param name="chunkSizeInBytes">分片大小（字节）</param>
        public static IEnumerable<(int Index, byte[] Data)> SplitFileIntoChunks(
            string filePath,
            int chunkSizeInBytes)
        {
            ValidateParameters(filePath, chunkSizeInBytes);

            using var fs = File.OpenRead(filePath);
            using var reader = new BinaryReader(fs);

            for (int chunkIndex = 0; ; chunkIndex++)
            {
                byte[] buffer = reader.ReadBytes(chunkSizeInBytes);
                if (buffer.Length == 0) yield break;
                yield return (chunkIndex, buffer);
            }
        }

        /// <summary>
        /// 将文件分片保存到本地目录
        /// </summary>
        /// <param name="filePath">源文件路径</param>
        /// <param name="chunkSizeInBytes">分片大小（字节）</param>
        /// <param name="outputDir">分片存储目录</param>
        /// <returns>生成的分片文件路径列表</returns>
        public static List<string> CreatePhysicalChunks(string filePath, int chunkSize, string outputDir, Action<int>? onProgress = null)
        {
            List<string> resultFiles = new();
            long totalSize = new FileInfo(filePath).Length;
            int partIndex = 0;
            long bytesReadTotal = 0;

            using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            byte[] buffer = new byte[chunkSize];
            int read;

            while ((read = input.Read(buffer, 0, chunkSize)) > 0)
            {
                string partFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(filePath)}_part{partIndex++}{Path.GetExtension(filePath)}");

                using var output = new FileStream(partFile, FileMode.Create, FileAccess.Write);
                output.Write(buffer, 0, read);

                bytesReadTotal += read;

                // 计算并回调进度百分比
                int percent = (int)((bytesReadTotal * 100.0) / totalSize);
                onProgress?.Invoke(percent);
                resultFiles.Add(partFile);
            }

            return resultFiles;
        }


        private static void ValidateParameters(string filePath, int chunkSize)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件未找到", filePath);
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "分片大小必须大于0");
        }
    }
}
