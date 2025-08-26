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

namespace TjdHelperWinUI.ViewModels
{
    public class EverythingPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<SearchResultItem> SearchResults { get; set; } = new();
        private EverythingHelper _helper;

        public EverythingPageViewModel()
        {
            _helper = new EverythingHelper();

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
                (obj) => DeleteCommandExecute(),
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
        private void DeleteCommandExecute()
        {
            if (SelectedItem == null) return;
            try
            {
                var filePath = Path.Combine(SelectedItem.Directory, SelectedItem.Name);
                if (File.Exists(filePath))
                    File.Delete(filePath);

                SearchResults.Remove(SelectedItem);
            }
            catch (Exception ex)
            {
                NotificationHelper.Show("错误", $"删除失败: {ex.Message}");
            }
        }
        #endregion
    }
}