
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Windows.Input;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public partial class MainWindowViewModel
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Binding Property
        /// <summary>
        /// 用户输入的search内容
        /// </summary>
        private string _strSearchInput;

        public string StrSearchInput
        {
            get { return _strSearchInput; }
            set
            {
                if (_strSearchInput != value)
                {
                    _strSearchInput = value;
                    OnPropertyChanged(nameof(StrSearchInput));
                    OnSearchTextChanged(_strSearchInput);
                }
            }
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        private ObservableCollection<PageInfo> _searchItemsResult = new ObservableCollection<PageInfo>();

        public ObservableCollection<PageInfo> SearchItemsResult
        {
            get { return _searchItemsResult; }
            set
            {
                if (_searchItemsResult != value)
                {
                    _searchItemsResult = value;
                    OnPropertyChanged(nameof(SearchItemsResult));
                }
            }
        }
        #endregion

        // 在 MainWindow.xaml.cs 或 ViewModel 中定义
        private ObservableCollection<PageInfo> _allPages = new ObservableCollection<PageInfo>
        {
            new PageInfo { Name = "主页", PageType = typeof(Pages.HomePage) },
            new PageInfo { Name = "Encryption", PageType = typeof(Pages.AddressHelperPage) },
            new PageInfo { Name = "WinErrCode", PageType = typeof(Pages.WinErrorCodePage) },
            new PageInfo { Name = "Address Calc", PageType = typeof(Pages.AddressHelperPage) }
        };

        public MainWindowViewModel()
        {

        }

        /// <summary>
        /// 根据内容搜索页面
        /// </summary>
        /// <param name="newText"></param>
        private void OnSearchTextChanged(string searchStr)
        {
            // 在这里处理搜索逻辑，例如执行搜索、过滤数据等
            // 根据输入过滤页面
            var query = searchStr.ToLower();
            var filteredPages = _allPages.Where(p => p.Name.ToLower().Contains(searchStr)).ToList();

            // 更新建议项
            SearchItemsResult.Clear();
            // 清空并重新填充集合
            foreach (var page in filteredPages)
            {
                SearchItemsResult.Add(page);
            }
        }

    }
}