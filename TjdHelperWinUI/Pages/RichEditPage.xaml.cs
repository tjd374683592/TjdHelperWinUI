using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Controls;
using Windows.Storage.Pickers;
using System.Collections.Generic;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RichEditPage : Page
    {
        public RichEditPageViewModel ViewModel { get; set; }
        public RichEditPage()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                ViewModel = App.Services.GetService<RichEditPageViewModel>();
                rootElement.DataContext = ViewModel;
            }

            // 等页面加载完成后再绑定 RichEditBox
            this.Loaded += RichEditPage_Loaded;

            // 默认显示 Text Edit
            Editor.Visibility = Visibility.Visible;
            ImageCropperControl.Visibility = Visibility.Collapsed;
        }

        private async Task LoadImage()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/GalleryHeaderImage.png"));
            await ImageCropperControl.LoadImageFromFile(file);
        }

        private void RichEditPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保 Editor 已创建，然后赋值给 ViewModel
            if (ViewModel != null && Editor != null)
            {
                ViewModel.Editor = Editor;
            }
        }

        private void Editor_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // 检查 Ctrl 键是否按下
            bool ctrlPressed = KeyHelper.IsCtrlPressed();

            if (!ctrlPressed) return;

            var richEditBox = sender as RichEditBox;
            var selection = richEditBox.Document.Selection;

            ITextCharacterFormat format = selection.CharacterFormat;

            float currentSize = format.Size;
            int delta = e.GetCurrentPoint(richEditBox).Properties.MouseWheelDelta;
            float step = 1f;

            float newSize = currentSize + (delta > 0 ? step : -step);
            newSize = Math.Max(1, newSize);

            format.Size = newSize;
            selection.CharacterFormat = format;

            e.Handled = true;
        }

        private void Editor_LostFocused(object sender, RoutedEventArgs e)
        {
            // 当 RichEditBox 失去焦点时，更新 ViewModel 的 Editor 引用
            if (ViewModel != null)
            {
                ViewModel.SaveSelection(); // 保存当前选区
            }
        }

        private void FontSizeBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // 尝试解析用户输入
                if (double.TryParse(FontSizeBox.Text, out double newSize))
                {
                    ViewModel.FontSize = newSize; // 会触发 ApplyFontSize()
                }

                // 阻止事件继续冒泡
                e.Handled = true;
            }
        }

        private void Editor_DragOver(object sender, DragEventArgs e)
        {
            // 显示复制效果
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private async void Editor_Drop(object sender, DragEventArgs e)
        {
            var file = await FileDropHandler.HandleDropAsync(e, OnRichEditTextLoaded);
            if (file != null)
            {
                ViewModel.CurrentFile = file;
                marqueeTextNotification.Visibility = Visibility.Visible;
            }
            else
            {
                ViewModel.CurrentFile = null;
                marqueeTextNotification.Visibility = Visibility.Collapsed;
            }
        }

        private async Task OnRichEditTextLoaded(string text)
        {
            Editor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, text);
            await Task.CompletedTask; // 占位，保持 async 方法签名
        }

        private void CommandBar_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tabbedBar = sender as TabbedCommandBar;
            var selectedTab = tabbedBar.SelectedItem as TabbedCommandBarItem;

            if (selectedTab == TextEditTab)
            {
                Editor.Visibility = Visibility.Visible;
                ImageCropperControl.Visibility = Visibility.Collapsed;
            }
            else if (selectedTab == ImageCropTab)
            {
                Editor.Visibility = Visibility.Collapsed;
                ImageCropperControl.Visibility = Visibility.Visible;

                LoadImage();
            }
        }

        private async void PickButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = await FileHelper.PickSingleFilePathAsync(App.MainWindow);
                if (!string.IsNullOrEmpty(filePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(filePath);
                    await ImageCropperControl.LoadImageFromFile(file);
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("加载图片失败", ex.Message);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ImageCropperControl == null)
                {
                    NotificationHelper.Show("错误", "没有可保存的图片");
                    return;
                }

                // 把裁剪后的图片保存到内存流
                using var stream = new InMemoryRandomAccessStream();
                await ImageCropperControl.SaveAsync(stream, BitmapFileFormat.Png);

                // 让用户选择保存路径
                await FileHelper.SaveImageAsync(App.MainWindow, stream);
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("保存失败", ex.Message);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ImageCropperControl.Reset();
        }

        private void ThumbPlacementCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageCropperControl == null) return;

            var selected = (ThumbPlacementCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            switch (selected)
            {
                case "All":
                    ImageCropperControl.ThumbPlacement = ThumbPlacement.All;
                    break;
                case "Corners":
                    ImageCropperControl.ThumbPlacement = ThumbPlacement.Corners;
                    break;
            }
        }

        private void CropShapeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageCropperControl == null) return;

            var selected = (CropShapeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            switch (selected)
            {
                case "Rectangular":
                    ImageCropperControl.CropShape = CropShape.Rectangular;
                    break;
                case "Circular":
                    ImageCropperControl.CropShape = CropShape.Circular;
                    break;
            }
        }

        private void AspectRatioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageCropperControl == null) return;

            var selected = (AspectRatioCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            switch (selected)
            {
                case "Custom":
                    ImageCropperControl.AspectRatio = 0; // 自由比例
                    break;
                case "Square":
                    ImageCropperControl.AspectRatio = 1; // 1:1
                    break;
                case "Landscape(16:9)":
                    ImageCropperControl.AspectRatio = 16.0 / 9.0;
                    break;
                case "Portrait(9:16)":
                    ImageCropperControl.AspectRatio = 9.0 / 16.0;
                    break;
                case "4:3":
                    ImageCropperControl.AspectRatio = 4.0 / 3.0;
                    break;
                case "3:2":
                    ImageCropperControl.AspectRatio = 3.0 / 2.0;
                    break;
            }
        }
    }
}