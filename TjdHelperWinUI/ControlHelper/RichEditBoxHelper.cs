using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace TjdHelperWinUI.ControlHelper
{
    public class RichEditBoxHelper
    {
        private static bool _isUpdating = false;

        public static string GetPlainText(DependencyObject obj) =>
            (string)obj.GetValue(PlainTextProperty);

        public static void SetPlainText(DependencyObject obj, string value) =>
            obj.SetValue(PlainTextProperty, value);

        public static readonly DependencyProperty PlainTextProperty =
            DependencyProperty.RegisterAttached(
                "PlainText",
                typeof(string),
                typeof(RichEditBoxHelper),
                new PropertyMetadata(string.Empty, OnPlainTextChanged));

        private static void OnPlainTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichEditBox richEditBox && e.NewValue is string newText)
            {
                if (_isUpdating) return;
                _isUpdating = true;

                // 加载纯文本到 RichEditBox
                richEditBox.Document.SetText(TextSetOptions.None, newText);

                _isUpdating = false;
            }
        }

        // 初始化时绑定TextChanged事件
        public static void Initialize(RichEditBox richEditBox)
        {
            richEditBox.TextChanged += (sender, e) =>
            {
                if (_isUpdating) return;
                _isUpdating = true;

                // 从 RichEditBox 获取纯文本
                richEditBox.Document.GetText(TextGetOptions.None, out string currentText);
                SetPlainText(richEditBox, currentText.TrimEnd('\r'));

                _isUpdating = false;
            };
        }
    }
}
