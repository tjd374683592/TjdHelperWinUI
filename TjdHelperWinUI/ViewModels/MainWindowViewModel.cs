﻿﻿﻿﻿﻿﻿﻿
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
        private readonly (string NameKey, Type PageType)[] _pageDefinitions =
        {
            ("Home", typeof(Pages.HomePage)),
            ("Encryption", typeof(Pages.EncryptHelperPage)),
            ("Encoding Converter", typeof(Pages.EnDecodePage)),
            ("Time Converter", typeof(Pages.TimeHelperPage)),
            ("File", typeof(Pages.FileHelperPage)),
            ("QR Code", typeof(Pages.QRCodePage)),
            ("Win Error Code", typeof(Pages.WinErrorCodePage)),
            ("Address Calc", typeof(Pages.AddressHelperPage)),
            ("Library", typeof(Pages.DebugNotePage)),
            ("Json Format", typeof(Pages.JsonFormatPage)),
            ("Media Converter", typeof(Pages.MediaConverterPage)),
            ("Markdown", typeof(Pages.MarkdownPage)),
            ("Rich Edit", typeof(Pages.RichEditPage)),
            ("Settings", typeof(Pages.SettingPage)),
            ("Postman", typeof(Pages.PostmanPage)),
            ("Control/Services", typeof(Pages.CplMscSearchPage)),
            ("Counter", typeof(Pages.SystemCounterPage)),
            ("DeepSeek", typeof(Pages.DeepSeekPage)),
            ("Network", typeof(Pages.NetworkPage)),
            ("Cim Explorer", typeof(Pages.CimPage))
        };

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

        private ObservableCollection<PageInfo> _allPages = new ObservableCollection<PageInfo>();

        public MainWindowViewModel()
        {
            ReloadLocalizedPageNames();
        }

        public void ReloadLocalizedPageNames()
        {
            _allPages = new ObservableCollection<PageInfo>(
                _pageDefinitions.Select(page => new PageInfo
                {
                    Name = LocalizationService.Translate(page.NameKey),
                    PageType = page.PageType
                }));

            OnSearchTextChanged(StrSearchInput ?? string.Empty);
        }

        /// <summary>
        /// 根据内容搜索页面
        /// </summary>
        /// <param name="newText"></param>
        private void OnSearchTextChanged(string searchStr)
        {
            var normalizedSearch = (searchStr ?? string.Empty).Trim();
            var filteredPages = string.IsNullOrEmpty(normalizedSearch)
                ? _allPages.ToList()
                : _allPages.Where(p => p.Name.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase)).ToList();

            SearchItemsResult.Clear();
            foreach (var page in filteredPages)
            {
                SearchItemsResult.Add(page);
            }
        }

    }
}
