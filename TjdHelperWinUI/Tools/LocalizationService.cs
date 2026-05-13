using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Globalization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using TjdHelperWinUI.ViewModels;

namespace TjdHelperWinUI.Tools
{
    public static class LocalizationService
    {
        private const string AppLanguageSettingKey = "AppLanguage";
        private static string _currentLanguageTag = "en-US";
        private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, string>> OriginalPropertyValues = new();

        private static readonly string[] TranslatableProperties =
        {
            "Text",
            "Header",
            "Label",
            "PlaceholderText",
            "Title",
            "Subtitle",
            "PrimaryButtonText",
            "SecondaryButtonText"
        };

        private static readonly string[] ObjectBackedTranslatableProperties =
        {
            "Content",
            "Header"
        };

        private static readonly Dictionary<string, TranslationItem> Translations = BuildTranslations();

        public static void Initialize()
        {
            _currentLanguageTag = LoadLanguageTag();
            SettingsHelper.SetSetting(AppLanguageSettingKey, _currentLanguageTag);
            ApplyLanguageOverride(_currentLanguageTag);
        }

        public static string GetCurrentLanguageTag()
        {
            return _currentLanguageTag;
        }

        public static bool IsChineseLanguage => GetCurrentLanguageTag().StartsWith("zh", StringComparison.OrdinalIgnoreCase);

        public static bool SetLanguage(string languageTag)
        {
            var normalizedLanguageTag = NormalizeLanguageTag(languageTag);
            var currentLanguageTag = _currentLanguageTag;
            var changed = !string.Equals(currentLanguageTag, normalizedLanguageTag, StringComparison.OrdinalIgnoreCase);

            SettingsHelper.SetSetting(AppLanguageSettingKey, normalizedLanguageTag);
            _currentLanguageTag = normalizedLanguageTag;
            ApplyLanguageOverride(normalizedLanguageTag);

            return changed;
        }

        public static string Translate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text ?? string.Empty;
            }

            if (Translations.TryGetValue(text, out var translation))
            {
                return IsChineseLanguage ? translation.ZhCn : translation.EnUs;
            }

            return text;
        }

        public static void RefreshApplicationLanguage()
        {
            if (App.Services?.GetService(typeof(MainWindowViewModel)) is MainWindowViewModel viewModel)
            {
                viewModel.ReloadLocalizedPageNames();
            }

            App.MainWindow?.RefreshLocalization();
        }

        public static void ApplyToWindow(Window window)
        {
            window.Title = Translate(window.Title);

            if (window.Content is FrameworkElement rootElement)
            {
                rootElement.Language = GetCurrentLanguageTag();
                ApplyToObject(rootElement);
            }
        }

        public static void ApplyToObject(object? instance)
        {
            if (instance == null)
            {
                return;
            }

            ApplyStringProperties(instance);
            ApplyObjectBackedStringProperties(instance);
            ApplySpecialCases(instance);

            if (instance is FrameworkElement frameworkElement)
            {
                ApplyToVisualChildren(frameworkElement);
            }
        }

        private static void ApplyToVisualChildren(FrameworkElement frameworkElement)
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(frameworkElement); i++)
                {
                    ApplyToObject(VisualTreeHelper.GetChild(frameworkElement, i));
                }
            }
            catch (COMException)
            {
                // 某些控件或三方控件内部对象并不支持按可视树递归访问，直接跳过其子节点。
            }
        }

        private static void ApplySpecialCases(object instance)
        {
            if (instance is ComboBox comboBox && comboBox.ItemsSource == null)
            {
                foreach (var item in comboBox.Items)
                {
                    ApplyToObject(item);
                }
            }

            if (instance is MenuBar menuBar)
            {
                foreach (var item in menuBar.Items)
                {
                    ApplyToObject(item);
                }
            }

            if (instance is MenuBarItem menuBarItem)
            {
                foreach (var item in menuBarItem.Items)
                {
                    ApplyToObject(item);
                }
            }

            if (instance is MenuFlyout menuFlyout)
            {
                foreach (var item in menuFlyout.Items)
                {
                    ApplyToObject(item);
                }
            }

            if (instance is NavigationView navigationView)
            {
                foreach (var item in navigationView.MenuItems)
                {
                    ApplyToObject(item);
                }

                if (navigationView.SettingsItem is NavigationViewItem settingsItem)
                {
                    ApplyToObject(settingsItem);
                }
            }

            if (instance is NavigationViewItem navigationViewItem)
            {
                foreach (var item in navigationViewItem.MenuItems)
                {
                    ApplyToObject(item);
                }
            }

            if (instance is Pivot pivot)
            {
                foreach (var item in pivot.Items)
                {
                    ApplyToObject(item);
                }
            }

            var contextFlyoutProperty = instance.GetType().GetProperty("ContextFlyout", BindingFlags.Public | BindingFlags.Instance);
            if (contextFlyoutProperty?.GetValue(instance) is object contextFlyout)
            {
                ApplyToObject(contextFlyout);
            }

            var flyoutProperty = instance.GetType().GetProperty("Flyout", BindingFlags.Public | BindingFlags.Instance);
            if (flyoutProperty?.GetValue(instance) is object flyout)
            {
                ApplyToObject(flyout);
            }

            var columnsProperty = instance.GetType().GetProperty("Columns", BindingFlags.Public | BindingFlags.Instance);
            if (columnsProperty?.GetValue(instance) is System.Collections.IEnumerable columns)
            {
                foreach (var column in columns)
                {
                    ApplyToObject(column);
                }
            }
        }

        private static void ApplyStringProperties(object instance)
        {
            var type = instance.GetType();
            var originalValues = OriginalPropertyValues.GetOrCreateValue(instance);

            foreach (var propertyName in TranslatableProperties)
            {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || property.PropertyType != typeof(string) || !property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                var currentValue = property.GetValue(instance) as string;
                if (string.IsNullOrWhiteSpace(currentValue))
                {
                    continue;
                }

                var originalValue = originalValues.GetOrAdd(propertyName, currentValue);
                var translatedValue = Translate(originalValue);
                if (!string.Equals(currentValue, translatedValue, StringComparison.Ordinal))
                {
                    property.SetValue(instance, translatedValue);
                }
            }
        }

        private static void ApplyObjectBackedStringProperties(object instance)
        {
            var type = instance.GetType();
            var originalValues = OriginalPropertyValues.GetOrCreateValue(instance);

            foreach (var propertyName in ObjectBackedTranslatableProperties)
            {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null || property.PropertyType != typeof(object) || !property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                if (property.GetValue(instance) is not string currentValue || string.IsNullOrWhiteSpace(currentValue))
                {
                    continue;
                }

                var cacheKey = $"object:{propertyName}";
                var originalValue = originalValues.GetOrAdd(cacheKey, currentValue);
                var translatedValue = Translate(originalValue);
                if (!string.Equals(currentValue, translatedValue, StringComparison.Ordinal))
                {
                    property.SetValue(instance, translatedValue);
                }
            }
        }

        private static void ApplyLanguageOverride(string languageTag)
        {
            var culture = new CultureInfo(languageTag);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            try
            {
                ApplicationLanguages.PrimaryLanguageOverride = languageTag;
            }
            catch (InvalidOperationException)
            {
                // WinUI 3 桌面应用在某些运行状态下可能无法设置系统级语言覆盖，
                // 此时保留应用内语言设置和界面刷新逻辑，避免切换功能整体失效。
            }
        }

        private static string NormalizeLanguageTag(string? languageTag)
        {
            return languageTag?.StartsWith("zh", StringComparison.OrdinalIgnoreCase) == true ? "zh-CN" : "en-US";
        }

        private static string LoadLanguageTag()
        {
            var savedLanguage = SettingsHelper.GetSetting<string>(AppLanguageSettingKey, string.Empty);
            return NormalizeLanguageTag(string.IsNullOrWhiteSpace(savedLanguage) ? GetDefaultLanguageTag() : savedLanguage);
        }

        private static string GetDefaultLanguageTag()
        {
            try
            {
                foreach (var language in ApplicationLanguages.Languages)
                {
                    if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        return "zh-CN";
                    }
                }
            }
            catch
            {
            }

            return "en-US";
        }

        private static Dictionary<string, TranslationItem> BuildTranslations()
        {
            var entries = new (string EnUs, string ZhCn)[]
            {
                ("Home", "首页"),
                ("Tools", "工具"),
                ("Windows", "Windows"),
                ("Preview", "预览"),
                ("Settings", "设置"),
                ("Search", "搜索"),
                ("search", "搜索"),
                ("Query", "查询"),
                ("TjdHelperWinUI Tool", "TjdHelperWinUI 工具"),
                ("Developed by Jerry Tang.", "由 Jerry Tang 开发。"),
                ("Application Settings", "应用设置"),
                ("App theme", "应用主题"),
                ("Navigation style", "导航样式"),
                ("App language", "应用语言"),
                ("Default", "默认"),
                ("Dark", "深色"),
                ("Light", "浅色"),
                ("Left", "左侧"),
                ("Top", "顶部"),
                ("DeepSeek", "DeepSeek"),
                ("API Key", "API Key"),
                ("Postman Main Page", "Postman 主页面"),
                ("Postman Project Url", "Postman 项目地址"),
                ("System Settings", "系统设置"),
                ("Theme", "主题"),
                ("English", "英语"),
                ("Chinese (Simplified)", "简体中文"),
                ("Encryption", "加密"),
                ("Encoding Converter", "编码转换"),
                ("Time Converter", "时间转换"),
                ("File", "文件"),
                ("QR Code", "二维码"),
                ("Media Converter", "媒体转换"),
                ("Postman", "Postman"),
                ("Library", "资料库"),
                ("Win Error Code", "Windows 错误码"),
                ("Counter", "系统监控"),
                ("Address Calc", "地址计算"),
                ("Control/Services", "控制面板/服务"),
                ("Network", "网络"),
                ("Cim Explorer", "Cim 浏览器"),
                ("Markdown", "Markdown"),
                ("Html", "HTML"),
                ("Json Format", "Json 格式化"),
                ("Rich Edit", "富文本编辑"),
                ("Everything", "Everything"),
                ("Text", "文本"),
                ("Text Edit", "文本编辑"),
                ("Image Crop", "图片裁剪"),
                ("Font Size", "字号"),
                ("Timestamp Converter", "时间戳转换"),
                ("Windows Error Code", "Windows 错误码"),
                ("Error Code:", "错误码："),
                ("Error Details:", "错误详情："),
                ("Example:5 Or 0x5", "示例：5 或 0x5"),
                ("Connection", "连接"),
                ("Signal", "信号"),
                ("IP Address", "IP 地址"),
                ("TCP/UDP Connection", "TCP/UDP 连接"),
                ("Search PID/Process/Port:", "搜索 PID/进程/端口："),
                ("Highlight", "高亮"),
                ("TCP Ports", "TCP 端口"),
                ("UDP Ports", "UDP 端口"),
                ("Status", "状态"),
                ("Description", "说明"),
                ("Meaning", "含义"),
                ("Local IP", "本地 IP"),
                ("Remote IP", "外部 IP"),
                ("Remote Port", "外部端口"),
                ("Process Name", "进程名"),
                ("Port", "端口"),
                ("Notice", "注意"),
                ("DeepSeek API Key updated successfully", "DeepSeek API Key 更新成功"),
                ("Postman Project Url updated successfully", "Postman Project Url 更新成功"),
                ("Please set DeepSeek API Key", "请设置 DeepSeek API Key"),
                ("Please set the Postman project URL first", "请先设置 Postman 项目地址"),
                ("Bookmarks", "书签"),
                ("Open Folder", "打开目录"),
                ("Run", "运行"),
                ("Open", "打开"),
                ("Name", "名称"),
                ("Directory", "目录"),
                ("Namespaces", "命名空间"),
                ("Class", "类"),
                ("Members", "成员"),
                ("Instance", "实例"),
                ("Date Modified", "修改时间"),
                ("New Session", "新会话"),
                ("Ask anything", "询问任何问题"),
                ("Show Length:", "显示数量："),
                ("Search...", "搜索..."),
                ("Search keywords...", "搜索关键字..."),
                ("Search Everything...", "搜索 Everything..."),
                ("Validate/Format Json", "校验/格式化 Json"),
                ("Compress", "压缩"),
                ("Escape", "转义"),
                ("Unescape", "去转义"),
                ("Clear", "清空"),
                ("Switch Theme", "切换主题"),
                ("Switch Monaco/TreeView", "切换 Monaco/TreeView"),
                ("Copy Value", "复制 Value"),
                ("Copy Key:Value", "复制 Key:Value"),
                ("Undo", "撤销"),
                ("Redo", "重做"),
                ("Paste", "粘贴"),
                ("Save", "保存"),
                ("Save As", "另存为"),
                ("Font Size", "字号"),
                ("Bold", "加粗"),
                ("Italic", "斜体"),
                ("Underline", "下划线"),
                ("Pick Image", "选择图片"),
                ("Reset", "重置"),
                ("CPU", "CPU"),
                ("Memory", "内存"),
                ("Disk Capacity", "磁盘容量"),
                ("Loading...", "加载中..."),
                ("Internet", "网络连接"),
                ("Metered", "按流量计费"),
                ("Common TCP Status", "常见 TCP 状态"),
                ("Search PID/Process/Port", "搜索 PID/进程/端口"),
                ("Id", "编号"),
                ("GUID", "GUID"),
                ("All", "全部"),
                ("Corners", "角点"),
                ("Rectangular", "矩形"),
                ("Circular", "圆形"),
                ("Custom", "自定义"),
                ("Square", "正方形"),
                ("Landscape(16:9)", "横向(16:9)"),
                ("Portrait(9:16)", "纵向(9:16)"),
                ("Base64 Encode", "Base64 编码"),
                ("Base64 Decode", "Base64 解码"),
                ("Base64 String", "Base64 字符串"),
                ("Encode", "编码"),
                ("Decode", "解码"),
                ("Encode", "编码"),
                ("Decode", "解码"),
                ("Create QR Code", "生成二维码"),
                ("Scan QR Code", "扫描二维码"),
                ("Audio To PCM", "音频转 PCM"),
                ("File Split", "文件分片"),
                ("File Unzip", "文件解压"),
                ("MD5 Hash", "MD5 哈希"),
                ("SHA1 Encryption", "SHA1 加密"),
                ("SHA1 Encryption", "SHA1 加密"),
                ("URL Encode / URL Decode", "URL 编码 / URL 解码"),
                ("Text -> Timestamp", "时间 -> 时间戳"),
                ("Timestamp -> Text", "时间戳 -> 时间"),
                ("Timezone", "时区"),
                ("Seconds (s)", "秒(s)"),
                ("Milliseconds (ms)", "毫秒(ms)"),
                ("type URL here", "请输入 URL"),
                ("Choose File", "选择文件"),
                ("Choose Audio File", "选择音频文件"),
                ("Choose File To Encode", "选择待编码文件"),
                ("Calculate File", "计算文件"),
                ("Calculate String", "计算字符串"),
                ("Base64 Encode Now", "Base64编码"),
                ("Base64 Decode Now", "Base64解码"),
                ("File path to encode as Base64", "需要计算Base64文件Path"),
                ("String to encode as Base64", "需要计算Base64的字符串"),
                ("Conversion Details", "转换详情"),
                ("Timestamp:", "时间戳："),
                ("Time:", "时间："),
                ("Enter time string: 1970-01-01 00:00:00", "输入时间str：1970-01-01 00:00:00"),
                ("Conversion Result:", "转换结果："),
                ("Choose a date", "选择日期"),
                ("Open Folder", "打开文件夹"),
                ("Convert to PCM", "转换成PCM格式"),
                ("PCM audio file path", "pcm格式音频文件Path"),
                ("Generate", "生成"),
                ("Copy File Name", "复制文件名"),
                ("Copy Full Path", "复制完整路径"),
                ("Clear All", "全部清空"),
                ("Delete", "删除"),
                ("Scan", "扫码"),
                ("Calculate", "计算"),
                ("Calculate File MD5", "计算文件MD5"),
                ("Calculate String MD5", "计算字符串MD5"),
                ("Start Split", "开始分片"),
                ("Start Unzip", "开始解压"),
                ("Leave blank if no password", "没有密码留空"),
                ("Password:", "密码："),
                ("Split Size:", "分片大小："),
                ("Split Details:", "分片详情："),
                ("File Selection", "文件选择"),
                ("File Path", "文件Path"),
                ("File Path Path", "文件路径 Path"),
                ("QR Code Path", "二维码 Path"),
                ("QR Code Content", "二维码内容"),
                ("QR Code Selection", "二维码选择"),
                ("QR Code String to Convert", "待转换二维码字符串"),
                ("Unit MB", "单位MB"),
                ("Log:", "Log："),
                ("MD5 Result:", "MD5 Result:"),
                ("Encrypt Result:", "Encrypt Result:"),
                ("String To Encrypt:", "String To Encrypt:"),
                ("String To Calc:", "String To Calc:"),
                ("GUID:", "GUID:"),
                ("Start Addr:", "Start Addr:"),
                ("End Addr:", "End Addr:"),
                ("VSize:", "VSize:"),
                ("Result here", "result here"),
                ("To be generated", "to be generated"),
                ("Type here", "type here"),
                ("PID", "PID"),
                ("Size", "大小")
            };

            var translations = new Dictionary<string, TranslationItem>(StringComparer.Ordinal);

            foreach (var entry in entries)
            {
                translations[entry.EnUs] = new TranslationItem(entry.ZhCn, entry.EnUs);
                translations[entry.ZhCn] = new TranslationItem(entry.ZhCn, entry.EnUs);
            }

            translations["Search"] = new TranslationItem("搜索", "Search");
            translations["search"] = new TranslationItem("搜索", "search");
            translations["查询"] = new TranslationItem("查询", "Query");
            translations["编码"] = new TranslationItem("编码", "Encode");
            translations["解码"] = new TranslationItem("解码", "Decode");
            translations["Search keywords..."] = new TranslationItem("搜索关键字...", "Search keywords...");
            translations["搜索关键字..."] = new TranslationItem("搜索关键字...", "Search keywords...");
            translations["Search Everything..."] = new TranslationItem("搜索 Everything...", "Search Everything...");
            translations["搜索 Everything..."] = new TranslationItem("搜索 Everything...", "Search Everything...");
            translations["请输入 URL"] = new TranslationItem("请输入 URL", "type URL here");
            translations["选择文件"] = new TranslationItem("选择文件", "Choose File");
            translations["选择音频文件"] = new TranslationItem("选择音频文件", "Choose Audio File");
            translations["计算文件"] = new TranslationItem("计算文件", "Calculate File");
            translations["计算字符串"] = new TranslationItem("计算字符串", "Calculate String");
            translations["Base64编码"] = new TranslationItem("Base64编码", "Base64 Encode Now");
            translations["Base64解码"] = new TranslationItem("Base64解码", "Base64 Decode Now");
            translations["需要计算Base64文件Path"] = new TranslationItem("需要计算Base64文件Path", "File path to encode as Base64");
            translations["需要计算Base64的字符串"] = new TranslationItem("需要计算Base64的字符串", "String to encode as Base64");
            translations["转换详情"] = new TranslationItem("转换详情", "Conversion Details");
            translations["时间戳："] = new TranslationItem("时间戳：", "Timestamp:");
            translations["时间："] = new TranslationItem("时间：", "Time:");
            translations["输入时间str：1970-01-01 00:00:00"] = new TranslationItem("输入时间str：1970-01-01 00:00:00", "Enter time string: 1970-01-01 00:00:00");
            translations["转换结果："] = new TranslationItem("转换结果：", "Conversion Result:");
            translations["选择日期"] = new TranslationItem("选择日期", "Choose a date");
            translations["询问任何问题"] = new TranslationItem("询问任何问题", "Ask anything");
            translations["显示数量："] = new TranslationItem("显示数量：", "Show Length:");
            translations["新会话"] = new TranslationItem("新会话", "New Session");
            translations["本地 IP"] = new TranslationItem("本地 IP", "Local IP");
            translations["外部 IP"] = new TranslationItem("外部 IP", "Remote IP");
            translations["外部端口"] = new TranslationItem("外部端口", "Remote Port");
            translations["进程名"] = new TranslationItem("进程名", "Process Name");
            translations["端口"] = new TranslationItem("端口", "Port");
            translations["状态"] = new TranslationItem("状态", "Status");
            translations["含义"] = new TranslationItem("含义", "Meaning");
            translations["说明"] = new TranslationItem("说明", "Description");
            translations["TCP 端口"] = new TranslationItem("TCP 端口", "TCP Ports");
            translations["UDP 端口"] = new TranslationItem("UDP 端口", "UDP Ports");
            translations["常见 TCP 状态"] = new TranslationItem("常见 TCP 状态", "Common TCP Status");
            translations["校验/格式化Json"] = new TranslationItem("校验/格式化 Json", "Validate/Format Json");
            translations["校验/格式化 Json"] = new TranslationItem("校验/格式化 Json", "Validate/Format Json");
            translations["压缩"] = new TranslationItem("压缩", "Compress");
            translations["转义"] = new TranslationItem("转义", "Escape");
            translations["去转义"] = new TranslationItem("去转义", "Unescape");
            translations["清空"] = new TranslationItem("清空", "Clear");
            translations["切换主题"] = new TranslationItem("切换主题", "Switch Theme");
            translations["切换Monaco/TreeView"] = new TranslationItem("切换 Monaco/TreeView", "Switch Monaco/TreeView");
            translations["切换 Monaco/TreeView"] = new TranslationItem("切换 Monaco/TreeView", "Switch Monaco/TreeView");
            translations["复制Value"] = new TranslationItem("复制 Value", "Copy Value");
            translations["复制Key:Value"] = new TranslationItem("复制 Key:Value", "Copy Key:Value");
            translations["磁盘容量"] = new TranslationItem("磁盘容量", "Disk Capacity");
            translations["加载中..."] = new TranslationItem("加载中...", "Loading...");
            translations["网络"] = new TranslationItem("网络", "Network");
            translations["时间 -> 时间戳"] = new TranslationItem("时间 -> 时间戳", "Text -> Timestamp");
            translations["时间戳 -> 时间"] = new TranslationItem("时间戳 -> 时间", "Timestamp -> Text");
            translations["时区"] = new TranslationItem("时区", "Timezone");
            translations["秒(s)"] = new TranslationItem("秒(s)", "Seconds (s)");
            translations["毫秒(ms)"] = new TranslationItem("毫秒(ms)", "Milliseconds (ms)");
            translations["打开文件夹"] = new TranslationItem("打开文件夹", "Open Folder");
            translations["转换成PCM格式"] = new TranslationItem("转换成PCM格式", "Convert to PCM");
            translations["pcm格式音频文件Path"] = new TranslationItem("pcm格式音频文件Path", "PCM audio file path");
            translations["打开"] = new TranslationItem("打开", "Open");
            translations["复制文件名"] = new TranslationItem("复制文件名", "Copy File Name");
            translations["复制完整路径"] = new TranslationItem("复制完整路径", "Copy Full Path");
            translations["生成"] = new TranslationItem("生成", "Generate");
            translations["全部清空"] = new TranslationItem("全部清空", "Clear All");
            translations["删除"] = new TranslationItem("删除", "Delete");
            translations["扫码"] = new TranslationItem("扫码", "Scan");
            translations["计算"] = new TranslationItem("计算", "Calculate");
            translations["计算文件MD5"] = new TranslationItem("计算文件MD5", "Calculate File MD5");
            translations["计算字符串MD5"] = new TranslationItem("计算字符串MD5", "Calculate String MD5");
            translations["开始分片"] = new TranslationItem("开始分片", "Start Split");
            translations["开始解压"] = new TranslationItem("开始解压", "Start Unzip");
            translations["没有密码留空"] = new TranslationItem("没有密码留空", "Leave blank if no password");
            translations["密码："] = new TranslationItem("密码：", "Password:");
            translations["分片大小："] = new TranslationItem("分片大小：", "Split Size:");
            translations["分片详情："] = new TranslationItem("分片详情：", "Split Details:");
            translations["文件选择"] = new TranslationItem("文件选择", "File Selection");
            translations["文件Path"] = new TranslationItem("文件Path", "File Path");
            translations["文件路径 Path"] = new TranslationItem("文件路径 Path", "File Path Path");
            translations["二维码 Path"] = new TranslationItem("二维码 Path", "QR Code Path");
            translations["二维码内容"] = new TranslationItem("二维码内容", "QR Code Content");
            translations["二维码选择"] = new TranslationItem("二维码选择", "QR Code Selection");
            translations["待转换二维码字符串"] = new TranslationItem("待转换二维码字符串", "QR Code String to Convert");
            translations["单位MB"] = new TranslationItem("单位MB", "Unit MB");
            translations["Log："] = new TranslationItem("Log：", "Log:");
            translations["SHA1 加密"] = new TranslationItem("SHA1 加密", "SHA1 Encryption");
            translations["Encrypt Result:"] = new TranslationItem("加密结果：", "Encrypt Result:");
            translations["加密结果："] = new TranslationItem("加密结果：", "Encrypt Result:");
            translations["MD5 Result:"] = new TranslationItem("MD5 结果：", "MD5 Result:");
            translations["MD5 结果："] = new TranslationItem("MD5 结果：", "MD5 Result:");
            translations["String To Encrypt:"] = new TranslationItem("待加密字符串：", "String To Encrypt:");
            translations["待加密字符串："] = new TranslationItem("待加密字符串：", "String To Encrypt:");
            translations["String To Calc:"] = new TranslationItem("待计算字符串：", "String To Calc:");
            translations["待计算字符串："] = new TranslationItem("待计算字符串：", "String To Calc:");
            translations["GUID:"] = new TranslationItem("GUID：", "GUID:");
            translations["GUID："] = new TranslationItem("GUID：", "GUID:");
            translations["Start Addr:"] = new TranslationItem("Start Addr:", "Start Addr:");
            translations["End Addr:"] = new TranslationItem("End Addr:", "End Addr:");
            translations["VSize:"] = new TranslationItem("VSize:", "VSize:");
            translations["result here"] = new TranslationItem("结果显示在这里", "result here");
            translations["结果显示在这里"] = new TranslationItem("结果显示在这里", "result here");
            translations["to be generated"] = new TranslationItem("待生成", "to be generated");
            translations["待生成"] = new TranslationItem("待生成", "to be generated");
            translations["type here"] = new TranslationItem("请在此输入", "type here");
            translations["请在此输入"] = new TranslationItem("请在此输入", "type here");
            translations["大小"] = new TranslationItem("大小", "Size");

            return translations;
        }

        private sealed record TranslationItem(string ZhCn, string EnUs);
    }
}
