using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using TjdHelperWinUI.Models;
using System.Text.Json;
using System.Reflection;
using Microsoft.UI;
using Windows.Web.UI.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DebugNotePage : Page
    {
        private List<Bookmark> bookmarks = new List<Bookmark>();

        public DebugNotePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            LoadBookmarks();
        }

        private async void LoadBookmarks()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\bookmarks.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();

                    if (bookmarks.Count > 0)
                    {
                        var menuItem = (MenuBarItem)FindName("BookmarksMenu");
                        menuItem.Items.Clear();

                        // 按分类分组
                        var grouped = bookmarks.GroupBy(b => b.Catagory);

                        foreach (var group in grouped)
                        {
                            // 创建一级菜单（分类）
                            var subMenu = new MenuFlyoutSubItem { Text = group.Key };

                            // 添加二级菜单项
                            foreach (var bookmark in group)
                            {
                                MenuFlyoutItem item = new MenuFlyoutItem
                                {
                                    Text = bookmark.Name,
                                    Tag = bookmark  // 保持Tag绑定
                                };
                                item.Click += Bookmark_Click;
                                subMenu.Items.Add(item);
                            }

                            menuItem.Items.Add(subMenu);
                        }
                    }
                }

                myWebView.Source = new Uri(((Bookmark)bookmarks.First()).Url);
            }
            catch (System.Exception ex)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load bookmarks: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        private void Bookmark_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                myWebView.Source = new Uri(((Bookmark)item.Tag).Url);
            }
        }
    }

}
