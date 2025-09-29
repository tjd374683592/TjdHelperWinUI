using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using TjdHelperWinUI.Tools;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace TjdHelperWinUI.ViewModels
{
    /// <summary>
    /// ViewModel for RichEditPage
    /// 提供 RichEditBox 的数据绑定和命令控制
    /// </summary>
    public class RichEditPageViewModel : INotifyPropertyChanged
    {
        #region PropertyChanged

        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 通知绑定属性发生改变
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region RichEditBox 引用

        private RichEditBox _editor;

        /// <summary>
        /// 绑定页面中的 RichEditBox 控件
        /// 通过 Page Loaded 事件赋值
        /// </summary>
        public RichEditBox Editor
        {
            get => _editor;
            set
            {
                if (_editor != value)
                {
                    _editor = value;
                    OnPropertyChanged(nameof(Editor));
                }
            }
        }

        #endregion

        #region 字体相关属性

        private string _selectedFont = "Calibri";

        /// <summary>
        /// 当前选择的字体
        /// 设置后会立即应用到 RichEditBox 光标或选中文本
        /// </summary>
        public string SelectedFont
        {
            get => _selectedFont;
            set
            {
                if (_selectedFont != value)
                {
                    _selectedFont = value;
                    OnPropertyChanged(nameof(SelectedFont));
                    ApplyFont();
                }
            }
        }

        private double _fontSize = 14;

        /// <summary>
        /// 当前字体大小
        /// 设置后会立即应用到 RichEditBox 光标或选中文本
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                    ApplyFontSize();
                }
            }
        }

        private bool _isBold;

        /// <summary>
        /// 是否加粗
        /// 设置后会立即应用到 RichEditBox 光标或选中文本
        /// </summary>
        public bool IsBold
        {
            get => _isBold;
            set
            {
                if (_isBold != value)
                {
                    _isBold = value;
                    OnPropertyChanged(nameof(IsBold));
                    ApplyBold();
                }
            }
        }

        private bool _isItalic;

        /// <summary>
        /// 是否斜体
        /// 设置后会立即应用到 RichEditBox 光标或选中文本
        /// </summary>
        public bool IsItalic
        {
            get => _isItalic;
            set
            {
                if (_isItalic != value)
                {
                    _isItalic = value;
                    OnPropertyChanged(nameof(IsItalic));
                    ApplyItalic();
                }
            }
        }

        private bool _isUnderline;

        /// <summary>
        /// 是否下划线
        /// 设置后会立即应用到 RichEditBox 光标或选中文本
        /// </summary>
        public bool IsUnderline
        {
            get => _isUnderline;
            set
            {
                if (_isUnderline != value)
                {
                    _isUnderline = value;
                    OnPropertyChanged(nameof(IsUnderline));
                    ApplyUnderline();
                }
            }
        }

        private int _selectionStart;
        private int _selectionLength;

        public void SaveSelection()
        {
            if (Editor != null)
            {
                var sel = Editor.Document.Selection;
                _selectionStart = sel.StartPosition;
                _selectionLength = sel.EndPosition - sel.StartPosition;
            }
        }

        public void RestoreSelection()
        {
            if (Editor != null)
            {
                var sel = Editor.Document.Selection;
                sel.SetRange(_selectionStart, _selectionStart + _selectionLength);
            }
        }

        private StorageFile _currentFile;

        public StorageFile CurrentFile
        {
            get => _currentFile;
            set
            {
                if (_currentFile != value)
                {
                    _currentFile = value;
                    OnPropertyChanged(nameof(CurrentFile));
                }
            }
        }
        #endregion

        #region 命令
        public ICommand UndoCommand { get; set; }
        public ICommand RedoCommand { get; set; }
        public ICommand PasteCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAsCommand { get; set; }

        #endregion

        /// <summary>
        /// 构造函数，初始化命令
        /// </summary>
        public RichEditPageViewModel()
        {
            UndoCommand = new RelayCommand(UndoCommandExecute);
            RedoCommand = new RelayCommand(RedoCommandExecute);
            PasteCommand = new RelayCommand(PasteCommandExecute);
            SaveCommand = new RelayCommand(SaveCommandExecute);
            SaveAsCommand = new RelayCommand(SaveAsCommandExecute);
        }

        #region 命令方法
        public async void SaveCommandExecute(object obj)
        {
            if (Editor == null) return;

            // 获取 RichEditBox 中的文本
            Editor.Document.GetText(TextGetOptions.None, out string text);

            if (_currentFile != null)
            {
                // 如果已有文件路径，直接覆盖保存
                File.WriteAllText(_currentFile.Path, text);
                NotificationHelper.Show("保存成功", $"文件已保存到 {_currentFile.Path}");
            }
            else
            {
                SaveAsCommandExecute(null);
            }
        }

        public async void SaveAsCommandExecute(object obj)
        {
            if (Editor == null) return;

            // 获取 RichEditBox 中的文本
            Editor.Document.GetText(TextGetOptions.None, out string text);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);

            // 调用 SaveFileAsync，允许用户输入任意扩展名
            await FileHelper.SaveFileAsync(
                App.MainWindow,
                data,
                "example", // 默认文件名（不带扩展名）
                new Dictionary<string, List<string>>() { { "文本文件", new List<string> { ".txt" } } }
            );
        }

        /// <summary>
        /// 执行撤销操作
        /// </summary>
        private void UndoCommandExecute(object obj)
        {
            Editor?.Document.Undo();
        }

        /// <summary>
        /// 执行重做操作
        /// </summary>
        private void RedoCommandExecute(object obj)
        {
            Editor?.Document.Redo();
        }

        /// <summary>
        /// 执行粘贴操作
        /// </summary>
        private void PasteCommandExecute(object obj)
        {
            Editor?.Document.Selection.Paste(0);
        }

        #endregion

        #region 属性应用方法

        /// <summary>
        /// 应用字体
        /// </summary>
        private void ApplyFont()
        {
            if (Editor != null && !string.IsNullOrEmpty(SelectedFont))
            {
                Editor.Document.Selection.CharacterFormat.Name = SelectedFont;
            }
        }

        /// <summary>
        /// 应用字体大小
        /// </summary>
        private void ApplyFontSize()
        {
            if (Editor != null)
            {
                RestoreSelection(); // 先恢复选区

                var selection = Editor.Document.Selection;
                var cf = selection.CharacterFormat;
                cf.Size = (float)FontSize;
                selection.CharacterFormat = cf;
            }
        }

        /// <summary>
        /// 应用加粗
        /// </summary>
        private void ApplyBold()
        {
            if (Editor != null)
            {
                Editor.Document.Selection.CharacterFormat.Bold = IsBold ? FormatEffect.On : FormatEffect.Off;
            }
        }

        /// <summary>
        /// 应用斜体
        /// </summary>
        private void ApplyItalic()
        {
            if (Editor != null)
            {
                Editor.Document.Selection.CharacterFormat.Italic = IsItalic ? FormatEffect.On : FormatEffect.Off;
            }
        }

        /// <summary>
        /// 应用下划线
        /// </summary>
        private void ApplyUnderline()
        {
            if (Editor != null)
            {
                Editor.Document.Selection.CharacterFormat.Underline = IsUnderline ? UnderlineType.Single : UnderlineType.None;
            }
        }

        #endregion
    }
}