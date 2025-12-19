using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private Process? _everythingProcess;

        public EverythingHelper()
        {
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

        #region SDK 封装
        public void Search(string keyword)
        {
            if (!string.IsNullOrEmpty(keyword))
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
        }

        public int ResultCount => Everything_GetNumResults();

        public string? GetResult(int index)
        {
            var sb = new StringBuilder(MAX_PATH);
            if (Everything_GetResultFullPathName(index, sb, sb.Capacity))
                return sb.ToString();
            return null;
        }

        public string GetResultSize(int index)
        {
            if (Everything_GetResultSize(index, out long size))
            {
                if (size == -1)
                    return "Folder";

                if (size < 1024) return $"{size} B";
                if (size < 1024 * 1024) return $"{size / 1024.0:F2} KB";
                if (size < 1024L * 1024 * 1024) return $"{size / 1024.0 / 1024.0:F2} MB";
                return $"{size / 1024.0 / 1024.0 / 1024.0:F2} GB";
            }
            return "0 B";
        }

        public DateTime? GetResultDateModified(int index)
        {
            if (Everything_GetResultDateModified(index, out long fileTime) && fileTime != 0)
            {
                try { return DateTime.FromFileTimeUtc(fileTime).ToLocalTime(); }
                catch { return null; }
            }
            return null;
        }

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
                    SearchResultType = GetResultSize(i) == "Folder" ? SearchResultItemType.Folder : SearchResultItemType.File,
                    DateModified = GetResultDateModified(i)
                };
            }
            return results;
        }
        #endregion

        #region Everything 启动逻辑（增强版）
        public bool EnsureEverythingRunning()
        {
            if (Process.GetProcessesByName("Everything").Any())
                return WaitUntilEverythingReady();

            if (!File.Exists(_everythingPath))
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = _everythingPath,
                Arguments = "-startup -minimized",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                _everythingProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动 Everything 失败: {ex.Message}");
                return false;
            }

            return WaitUntilEverythingReady();
        }

        private static bool WaitUntilEverythingReady(int timeoutMs = 10000)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (IsEverythingAvailable())
                    return true;

                Thread.Sleep(500);
            }

            Debug.WriteLine("等待 Everything IPC 超时。");
            return false;
        }

        private static bool IsEverythingAvailable()
        {
            try
            {
                Everything_SetSearch("test");
                Everything_Query(true);
                return Everything_GetNumResults() >= 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 清理
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

        ~EverythingHelper() => Dispose();
        #endregion
    }
}
