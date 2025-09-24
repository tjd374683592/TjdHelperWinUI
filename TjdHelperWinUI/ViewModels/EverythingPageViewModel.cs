using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Tools;
using Windows.ApplicationModel.DataTransfer;

namespace TjdHelperWinUI.ViewModels
{
    public class EverythingPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<SearchResultItem> SearchResults { get; set; } = new();
        private readonly EverythingHelper _helper;
        private readonly IMessageService _messageService;

        public EverythingPageViewModel(IMessageService? messageService = null)
        {
            _helper = new EverythingHelper();
            _messageService = messageService ?? new MessageService();

            StartSearchingCommand = new RelayCommand(async (obj) => await StartSearchingCommandExecute());

            OpenFileCommand = new RelayCommand(
                (obj) => OpenFileCommandExecute(),
                (obj) => SelectedItem != null
            );

            OpenDirectoryCommand = new RelayCommand(
                (obj) => OpenDirectoryCommandExecute(),
                (obj) => SelectedItem != null
            );

            DeleteCommand = new RelayCommand(
                async (obj) => await DeleteCommandExecuteAsync(),
                (obj) => SelectedItem != null
            );

            CopyFileNameCommand = new RelayCommand(
                (obj) => CopyFileNameCommandExecute(),
                (obj) => SelectedItem != null
            );

            CopyFullDirectoryCommand = new RelayCommand(
                (obj) => CopyFullDirectoryCommandExecute(),
                (obj) => SelectedItem != null
            );

            CommandLineCommand = new RelayCommand(
                (obj) => CommandLineCommandExecute(),
                (obj) => SelectedItem != null
            );
        }

        #region 搜索相关
        private string _searchKeywords;
        public string SearchKeywords
        {
            get => _searchKeywords;
            set
            {
                if (_searchKeywords != value)
                {
                    _searchKeywords = value;
                    OnPropertyChanged(nameof(SearchKeywords));
                }
            }
        }

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    OnPropertyChanged(nameof(IsSearching));
                }
            }
        }

        public ICommand StartSearchingCommand { get; set; }

        private async Task StartSearchingCommandExecute()
        {
            try
            {
                IsSearching = true;

                var results = await Task.Run(() =>
                {
                    _helper.EnsureEverythingRunning();
                    _helper.Search(SearchKeywords);
                    return _helper.GetAllResults(50);
                });

                SearchResults.Clear();
                foreach (var item in results.Select((r, i) => new SearchResultItem
                {
                    Id = i + 1,
                    Name = r.Name.Trim(),
                    Directory = r.Directory.Trim(),
                    Size = r.Size,
                    DateModified = r.DateModified
                }))
                {
                    SearchResults.Add(item);
                }
            }
            finally
            {
                IsSearching = false;
            }
        }
        #endregion

        #region 选中行 + 命令
        private SearchResultItem _selectedItem;
        public SearchResultItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));

                    // 通知命令 CanExecute 改变
                    ((RelayCommand)OpenFileCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)OpenDirectoryCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand OpenFileCommand { get; set; }
        private void OpenFileCommandExecute()
        {
            if (SelectedItem == null) return;

            string fullPath = Path.Combine(SelectedItem.Directory, SelectedItem.Name);

            if (File.Exists(fullPath))
            {
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            else
            {
                NotificationHelper.Show("错误", $"文件不存在: {fullPath}");
            }
        }

        public ICommand OpenDirectoryCommand { get; set; }
        private void OpenDirectoryCommandExecute()
        {
            if (SelectedItem == null) return;

            FileHelper.OpenFolder(SelectedItem.Directory);
        }

        public ICommand DeleteCommand { get; set; }
        private async Task DeleteCommandExecuteAsync()
        {
            if (SelectedItem == null) return;

            bool confirm = await _messageService.ShowConfirmDialogAsync(
                "删除确认",
                $"确定要删除文件 \"{SelectedItem.Name}\" 吗？"
            );

            if (!confirm) return;

            try
            {
                var filePath = Path.Combine(SelectedItem.Directory, SelectedItem.Name);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                SearchResults.Remove(SelectedItem);
            }
            catch (Exception ex)
            {
                await _messageService.ShowMessageAsync("错误", $"删除失败: {ex.Message}");
            }
        }

        public ICommand CopyFileNameCommand { get; set; }
        private void CopyFileNameCommandExecute()
        {
            if (SelectedItem == null) return;

            var dataPackage = new DataPackage();
            dataPackage.SetText(SelectedItem.Name);
            Clipboard.SetContent(dataPackage);

            NotificationHelper.Show("已复制", $"文件名已复制到剪贴板: {SelectedItem.Name}");
        }

        public ICommand CopyFullDirectoryCommand { get; set; }
        private void CopyFullDirectoryCommandExecute()
        {
            if (SelectedItem == null) return;

            string fullPath = Path.Combine(SelectedItem.Directory, SelectedItem.Name);

            var dataPackage = new DataPackage();
            dataPackage.SetText(fullPath);
            Clipboard.SetContent(dataPackage);

            NotificationHelper.Show("已复制", $"完整路径已复制到剪贴板: {fullPath}");
        }


        public ICommand CommandLineCommand { get; set; }
        private void CommandLineCommandExecute()
        {
            if (SelectedItem == null) return;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K cd /d \"{SelectedItem.Directory}\"",
                UseShellExecute = true // 必须为true，才能打开独立窗口
            };

            Process.Start(psi);
        }
        #endregion
    }
}