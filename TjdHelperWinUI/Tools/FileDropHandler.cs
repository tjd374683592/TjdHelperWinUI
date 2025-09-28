using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace TjdHelperWinUI.Tools
{
    public static class FileDropHandler
    {
        /// <summary>
        /// 处理拖拽文件
        /// 返回 StorageFile（如果是文本）或 null
        /// </summary>
        public static async Task<StorageFile?> HandleDropAsync(
            DragEventArgs e,
            Func<string, Task> onTextLoaded,
            Func<string, Task>? onHighlightLang = null)
        {
            try
            {
                if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                    return null;

                var items = await e.DataView.GetStorageItemsAsync();
                var file = items.FirstOrDefault() as StorageFile;
                if (file == null) return null;

                string contentType = file.ContentType.ToLower();

                // 非文本文件用系统打开
                if (contentType == "application/pdf" ||
                    contentType.StartsWith("image/") ||
                    contentType.StartsWith("video/") ||
                    contentType == "application/x-zip-compressed" ||
                    contentType.StartsWith("audio/"))
                {
                    bool success = await Launcher.LaunchFileAsync(file);
                    if (!success)
                        NotificationHelper.Show("无法打开文件: " + file.Name);
                    return null;
                }

                // 文本文件读取内容
                string text = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                await onTextLoaded(text);

                if (onHighlightLang != null)
                {
                    string lang = GetLanguageFromExtension(Path.GetExtension(file.Name));
                    await onHighlightLang(lang);
                }

                return file; // 返回文件对象
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("文件读取失败: " + ex.Message);
                return null;
            }
            finally
            {
                e.Handled = true;
            }
        }

        private static string GetLanguageFromExtension(string ext)
        {
            return ext.ToLower() switch
            {
                ".txt" => "plaintext",
                ".cs" => "csharp",
                ".py" => "python",
                ".js" => "javascript",
                ".json" => "json",
                ".html" => "html",
                ".css" => "css",
                _ => "plaintext"
            };
        }
    }
}
