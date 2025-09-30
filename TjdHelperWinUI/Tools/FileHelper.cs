using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

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

        public static void OpenFolder(string path)
        {
            try
            {
                // 先尝试处理文件
                if (TryHandleFile(path))
                    return;

                // 再尝试处理目录
                if (TryHandleDirectory(path))
                    return;

                // 如果文件和目录都无法访问，提示路径不存在或无权限
                NotificationHelper.Show("错误", $"路径不存在或无法访问: {path}");
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("错误", $"打开资源管理器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试处理文件，如果存在且可访问，打开资源管理器
        /// </summary>
        private static bool TryHandleFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (CanAccessDirectory(fileInfo.DirectoryName))
                    {
                        Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
                    }
                    else
                    {
                        NotificationHelper.Show("错误", $"文件所在目录存在但没有访问权限: {fileInfo.DirectoryName}");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("错误", ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 尝试处理目录，如果存在且可访问，打开资源管理器
        /// </summary>
        private static bool TryHandleDirectory(string path)
        {
            try
            {
                // 尝试列举目录内容判断是否存在
                Directory.EnumerateFileSystemEntries(path).FirstOrDefault();

                // 如果能枚举说明目录存在且可访问
                Process.Start("explorer.exe", $"/select,\"{path}\"");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // 存在但无权限访问
                NotificationHelper.Show("错误", $"文件夹存在但没有访问权限: {path}");
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                // 目录不存在
                NotificationHelper.Show("错误", "文件夹不存在");
                return false;
            }
            catch
            {
                // 其他异常当作无法访问
                NotificationHelper.Show("错误", $"无法访问文件夹: {path}");
                return true;
            }
        }

        /// <summary>
        /// 判断目录是否可访问（存在且有权限）
        /// </summary>
        private static bool CanAccessDirectory(string directoryPath)
        {
            try
            {
                Directory.EnumerateFileSystemEntries(directoryPath).FirstOrDefault();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch
            {
                return true;
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

        public static async Task SaveImageAsync(Window window, InMemoryRandomAccessStream imageStream)
        {
            var savePicker = new FileSavePicker();

            // 关键：在 WinUI 3 里必须手动绑定窗口句柄
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(savePicker, hWnd);

            // 设置保存文件类型和默认文件名
            savePicker.FileTypeChoices.Add("PNG 图片", new List<string>() { ".png" });
            savePicker.SuggestedFileName = "cropped_image" + DateTime.Now.ToString("yyyyMMddHHmmss");

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    imageStream.Seek(0);
                    await RandomAccessStream.CopyAndCloseAsync(
                        imageStream.GetInputStreamAt(0),
                        fileStream.GetOutputStreamAt(0));
                }

                NotificationHelper.Show("成功", $"图片已保存到: {file.Path}");
                OpenFolder(Path.GetDirectoryName(file.Path) ?? string.Empty);
            }
            else
            {
                NotificationHelper.Show("提示", "保存已取消");
            }
        }

        public static async Task SaveFileAsync(Window window, byte[] data, string defaultFileName, Dictionary<string, List<string>> fileTypes)
        {
            var savePicker = new FileSavePicker();

            // 绑定 WinUI 3 窗口句柄
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(savePicker, hWnd);

            // 设置默认文件名
            savePicker.SuggestedFileName = defaultFileName;

            // 设置保存类型
            foreach (var kv in fileTypes)
            {
                savePicker.FileTypeChoices.Add(kv.Key, kv.Value);
            }

            // 弹出保存对话框
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                using var fs = await file.OpenStreamForWriteAsync();
                await fs.WriteAsync(data, 0, data.Length);

                NotificationHelper.Show("成功", $"文件已保存到: {file.Path}");
                FileHelper.OpenFolder(Path.GetDirectoryName(file.Path) ?? string.Empty);
            }
            else
            {
                NotificationHelper.Show("提示", "保存已取消");
            }
        }
    }
}