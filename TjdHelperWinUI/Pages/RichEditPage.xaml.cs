using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;
using Windows.System;

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
    }
}
