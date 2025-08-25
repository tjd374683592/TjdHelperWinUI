using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Tools;
using TjdHelperWinUI.ViewModels;

namespace TjdHelperWinUI.Pages
{
    public sealed partial class EverythingPage : Page
    {
        

        public EverythingPage()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                rootElement.DataContext = App.Services.GetService<EverythingPageViewModel>();
            }
        }

        private void SearchTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                App.Services.GetService<EverythingPageViewModel>().StartSearchingCommand.Execute(null);
            }
        }
    }
}
