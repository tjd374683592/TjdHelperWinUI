using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace TjdHelperWinUI.Tools
{
    public static class FileDropHandler
    {
        public static async Task HandleDropAsync(
            DragEventArgs e,
            Func<string, Task> onTextLoaded,
            Func<string, Task>? onHighlightLang = null)
        {
            try
            {
                if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                    return;

                var items = await e.DataView.GetStorageItemsAsync();
                var file = items.FirstOrDefault() as StorageFile;

                if (file == null) return;

                string contentType = file.ContentType.ToLower();

                // 如果是非文本（pdf、图片、视频、压缩包、音频）→ 系统默认程序打开
                if (contentType == "application/pdf" ||
                    contentType.StartsWith("image/") ||
                    contentType.StartsWith("video/") ||
                    contentType == "application/x-zip-compressed" ||
                    contentType.StartsWith("audio/"))
                {
                    bool success = await Launcher.LaunchFileAsync(file);
                    if (!success)
                    {
                        NotificationHelper.Show("无法打开文件: " + file.Name);
                    }
                    return;
                }

                // 默认按文本读取
                //string text = await FileIO.ReadTextAsync(file);
                string text = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);

                await onTextLoaded(text);

                if (onHighlightLang != null)
                {
                    string lang = GetLanguageFromExtension(Path.GetExtension(file.Name));
                    await onHighlightLang(lang);
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("文件读取失败: " + ex.Message);
            }
            finally
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 根据文件扩展名获取对应的代码语言
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        private static string GetLanguageFromExtension(string ext)
        {
            return ext.ToLower() switch
            {
                // Web & Frontend
                ".html" => "html",
                ".htm" => "html",
                ".css" => "css",
                ".scss" => "scss",
                ".sass" => "scss",
                ".less" => "less",
                ".xml" => "xml",
                ".xhtml" => "xml",
                ".svg" => "xml",

                // JavaScript & TypeScript
                ".js" => "javascript",
                ".jsx" => "javascript",
                ".ts" => "typescript",
                ".tsx" => "typescript",

                // JSON / YAML
                ".json" => "json",
                ".jsonc" => "jsonc",
                ".yaml" => "yaml",
                ".yml" => "yaml",

                // C/C++
                ".c" => "c",
                ".cpp" => "cpp",
                ".cc" => "cpp",
                ".cxx" => "cpp",
                ".h" => "cpp",
                ".hpp" => "cpp",

                // C#
                ".cs" => "csharp",

                // Java
                ".java" => "java",

                // Python
                ".py" => "python",

                // Go
                ".go" => "go",

                // Rust
                ".rs" => "rust",

                // PHP
                ".php" => "php",

                // Ruby
                ".rb" => "ruby",

                // Shell
                ".sh" => "shell",
                ".bash" => "shell",

                // PowerShell
                ".ps1" => "powershell",
                ".psm1" => "powershell",

                // SQL
                ".sql" => "sql",

                // Markdown & 文本
                ".md" => "markdown",
                ".markdown" => "markdown",
                ".txt" => "plaintext",
                ".log" => "plaintext",

                // INI / TOML / Config
                ".ini" => "ini",
                ".toml" => "toml",
                ".env" => "ini",

                // Lua
                ".lua" => "lua",

                // Dart
                ".dart" => "dart",

                // Kotlin
                ".kt" => "kotlin",
                ".kts" => "kotlin",

                // Swift
                ".swift" => "swift",

                // R
                ".r" => "r",

                // Objective-C
                ".m" => "objective-c",
                ".mm" => "objective-cpp",

                // Assembly
                ".asm" => "asm",
                ".s" => "asm",

                // F#
                ".fs" => "fsharp",

                // Docker
                "dockerfile" => "dockerfile",
                ".dockerfile" => "dockerfile",

                // Makefile
                "makefile" => "makefile",
                ".mk" => "makefile",

                // Misc
                ".bat" => "bat",
                ".cmd" => "bat",

                _ => "plaintext"
            };
        }
    };
}