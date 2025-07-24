using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public static class FileSplitter
    {
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
