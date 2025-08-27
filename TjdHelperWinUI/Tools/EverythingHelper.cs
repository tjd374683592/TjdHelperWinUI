using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using TjdHelperWinUI.Models;

namespace TjdHelperWinUI.Tools
{
    /// <summary>
    /// Everything SDK 封装，通过 IPC 与 Everything 通讯
    /// </summary>
    public class EverythingHelper : IDisposable
    {
        private const int MAX_PATH = 260;

        private readonly string _everythingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Tools\Everything.exe");
        private readonly string _everythingdllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Tools\Everything64.dll");

        private Process? _everythingProcess;  // 仅记录由我们启动的 Everything

        public EverythingHelper()
        {
            // 设置 DLL 搜索路径（确保可以找到 Resources\Tools\Everything64.dll）
            string dllDir = Path.GetDirectoryName(_everythingdllPath)!;
            if (!Directory.Exists(dllDir))
                throw new DirectoryNotFoundException($"DLL 目录不存在: {dllDir}");

            SetDllDirectory(dllDir);

            if (!EnsureEverythingRunning())
                throw new InvalidOperationException("无法启动或连接 Everything.exe");
        }

        #region P/Invoke Everything SDK

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_SetSearch(string search);

        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(uint flags);

        [DllImport("Everything64.dll")]
        private static extern void Everything_Query(bool wait);

        [DllImport("Everything64.dll")]
        private static extern int Everything_GetNumResults();

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern bool Everything_GetResultFullPathName(int index, StringBuilder buf, int bufLen);

        [DllImport("Everything64.dll")]
        private static extern bool Everything_GetResultSize(int index, out long size);

        [DllImport("Everything64.dll")]
        private static extern bool Everything_GetResultDateModified(int index, out long fileTime);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);

        #endregion

        #region 常量定义

        private const uint EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const uint EVERYTHING_REQUEST_PATH = 0x00000002;
        private const uint EVERYTHING_REQUEST_SIZE = 0x00000010;
        private const uint EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;

        #endregion

        #region SDK 封装方法

        /// <summary>
        /// 执行搜索
        /// </summary>
        public void Search(string keyword)
        {
            Everything_SetSearch(keyword);
            Everything_SetRequestFlags(
                EVERYTHING_REQUEST_FILE_NAME |
                EVERYTHING_REQUEST_PATH |
                EVERYTHING_REQUEST_SIZE |
                EVERYTHING_REQUEST_DATE_MODIFIED
            );
            Everything_Query(true);
        }

        /// <summary>
        /// 获取结果数量
        /// </summary>
        public int ResultCount => Everything_GetNumResults();

        /// <summary>
        /// 获取指定索引的完整路径
        /// </summary>
        public string? GetResult(int index)
        {
            var sb = new StringBuilder(MAX_PATH);
            if (Everything_GetResultFullPathName(index, sb, sb.Capacity))
                return sb.ToString();
            return null;
        }

        /// <summary>
        /// 获取指定索引的文件大小（字节）
        /// </summary>
        public long GetResultSize(int index)
        {
            if (Everything_GetResultSize(index, out long size))
                return size / 1024; // 转换为 KB
            return 0;
        }

        /// <summary>
        /// 获取指定索引的修改时间
        /// </summary>
        public DateTime? GetResultDateModified(int index)
        {
            if (Everything_GetResultDateModified(index, out long fileTime) && fileTime != 0)
            {
                try
                {
                    return DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有结果，可限制前 N 条
        /// </summary>
        public SearchResultItem[] GetAllResults(int maxResults = 0)
        {
            int count = ResultCount;
            if (maxResults > 0)
                count = Math.Min(count, maxResults);

            var results = new SearchResultItem[count];
            for (int i = 0; i < count; i++)
            {
                var fullPath = GetResult(i) ?? string.Empty;
                results[i] = new SearchResultItem
                {
                    Id = i + 1,
                    Name = Path.GetFileName(fullPath),
                    Directory = Path.GetDirectoryName(fullPath) ?? string.Empty,
                    Size = GetResultSize(i),
                    DateModified = GetResultDateModified(i)
                };
            }
            return results;
        }

        #endregion

        #region Everything 自动启动逻辑

        /// <summary>
        /// 确保 Everything.exe 已经在运行
        /// </summary>
        public bool EnsureEverythingRunning()
        {
            // 如果有服务，尝试启动（但不 return）
            TryEnsureServiceRunning("Everything");

            // 启动 Everything.exe (隐藏)
            if (!File.Exists(_everythingPath))
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = _everythingPath,
                Arguments = "-startup",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            _everythingProcess = Process.Start(psi);

            // 等待 Everything 初始化（最多 5 秒）
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 5000)
            {
                if (Process.GetProcessesByName("Everything").Length > 0)
                    return true;
                Thread.Sleep(200);
            }

            return false;
        }

        /// <summary>
        /// 确保服务运行（如果存在）
        /// </summary>
        private void TryEnsureServiceRunning(string serviceName)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.Show(ex.Message);
            }
        }

        #endregion

        #region 资源清理

        public void Dispose()
        {
            try
            {
                if (_everythingProcess != null && !_everythingProcess.HasExited)
                {
                    _everythingProcess.Kill(true);
                    _everythingProcess.Dispose();
                }
            }
            catch { }
        }

        ~EverythingHelper()
        {
            Dispose();
        }

        #endregion
    }
}