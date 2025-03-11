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
using Windows.UI.ApplicationSettings;
using Microsoft.Extensions.DependencyInjection;
using TjdHelperWinUI.ViewModels;
using Microsoft.Web.WebView2.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        public TestPage()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // �� DI �����л�ȡ ViewModel
                var viewModel = App.Services.GetService<TestPageViewModel>();
                rootElement.DataContext = App.Services.GetService<TestPageViewModel>();
            }
        }

        private void MyTabView_AddTabButtonClick(TabView sender, object args)
        {
            var newTab = new TabViewItem
            {
                Header = "New Tab"
            };

            var webView = new WebView2
            {
                Source = new Uri("https://www.onenote.com/notebooks")
            };

            newTab.Content = webView;
            sender.TabItems.Add(newTab);
        }
    }
}
