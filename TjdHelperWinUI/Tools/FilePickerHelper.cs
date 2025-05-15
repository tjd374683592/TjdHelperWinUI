using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace TjdHelperWinUI.Tools
{
    public static class FilePickerHelper
    {
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
    }
}
