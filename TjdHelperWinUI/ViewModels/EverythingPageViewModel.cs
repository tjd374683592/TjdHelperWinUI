using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<SearchResultItem> SearchResults { get; set; } = new();

        private EverythingHelper _helper;

        /// <summary>
        /// 搜索关键词
        /// </summary>
        private string _searchKeywords;

        public string SearchKeywords
        {
            get { return _searchKeywords; }
            set
            {
                if (_searchKeywords != value)
                {
                    _searchKeywords = value;
                    OnPropertyChanged(nameof(SearchKeywords));
                }
            }
        }

        public ICommand StartSearchingCommand { get; set; }

        public EverythingPageViewModel()
        {
            _helper = new EverythingHelper();

            StartSearchingCommand = new RelayCommand(StartSearchingCommandExecute);
        }

        private void StartSearchingCommandExecute(object obj)
        {
            // 清空现有结果
            SearchResults.Clear();
            _helper.EnsureEverythingRunning();

            // 执行搜索
            _helper.Search(SearchKeywords);

            // 获取前 50 条结果
            var searchItemArray = _helper.GetAllResults(50);

            // 遍历填充 ObservableCollection
            for (int i = 0; i < searchItemArray.Length; i++)
            {
                var item = searchItemArray[i];

                // 添加到 SearchResults 集合
                SearchResults.Add(new SearchResultItem
                {
                    Id = i + 1,
                    Name = item.Name.Trim(),
                    Directory = item.Directory.Trim(),
                    Size = item.Size,
                    DateModified = item.DateModified
                });
            }
        }
    }
}
