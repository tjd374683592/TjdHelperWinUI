using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace TjdHelperWinUI.Tools
{
    public static class SettingsHelper
    {
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private static Dictionary<string, object> _settings;

        static SettingsHelper()
        {
            LoadSettings();
        }

        // 检测是否运行在打包模式下
        private static bool IsPackaged()
        {
            try
            {
                var package = Windows.ApplicationModel.Package.Current;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 获取设置值（兼容打包和解包模式）
        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            // 优先尝试打包模式
            if (IsPackaged())
            {
                try
                {
                    var settings = ApplicationData.Current.LocalSettings;
                    if (settings.Values.ContainsKey(key))
                    {
                        var value = settings.Values[key];
                        if (value is T typedValue)
                            return typedValue;
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                catch
                {
                    // 如果打包模式失败，回退到配置文件
                }
            }

            // 解包模式或打包模式失败时使用配置文件
            if (_settings.ContainsKey(key))
            {
                try
                {
                    var value = _settings[key];
                    if (value is JsonElement element)
                    {
                        return element.Deserialize<T>();
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        // 设置值（兼容打包和解包模式）
        public static void SetSetting<T>(string key, T value)
        {
            // 优先尝试打包模式
            if (IsPackaged())
            {
                try
                {
                    var settings = ApplicationData.Current.LocalSettings;
                    settings.Values[key] = value;
                    return; // 如果成功，直接返回
                }
                catch
                {
                    // 如果打包模式失败，回退到配置文件
                }
            }

            // 解包模式或打包模式失败时使用配置文件
            _settings[key] = value;
            SaveSettings();
        }

        private static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                              ?? new Dictionary<string, object>();
                }
                else
                {
                    _settings = new Dictionary<string, object>();
                }
            }
            catch
            {
                _settings = new Dictionary<string, object>();
            }
        }

        private static void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            }
        }
    }
}