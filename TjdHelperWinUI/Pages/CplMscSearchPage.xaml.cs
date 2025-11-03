using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TjdHelperWinUI.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CplMscSearchPage : Page
    {
        public CplMscSearchPageViewModel ViewModel { get; set; }

        public CplMscSearchPage()
        {
            InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                ViewModel = App.Services.GetService<CplMscSearchPageViewModel>();
                rootElement.DataContext = ViewModel;
            }
        }

        private void CplResultsGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var depObj = e.OriginalSource as DependencyObject;

            while (depObj != null)
            {
                // 找到有 DataContext 的元素
                var dataContext = (depObj as FrameworkElement)?.DataContext;
                if (dataContext != null && ((FrameworkElement)depObj).DataContext is TjdHelperWinUI.Models.SearchResultItem)
                {
                    DgCplResults.SelectedItem = dataContext;
                    break;
                }

                depObj = VisualTreeHelper.GetParent(depObj);
            }
        }

        private void MscResultsGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var depObj = e.OriginalSource as DependencyObject;

            while (depObj != null)
            {
                // 找到有 DataContext 的元素
                var dataContext = (depObj as FrameworkElement)?.DataContext;
                if (dataContext != null && ((FrameworkElement)depObj).DataContext is TjdHelperWinUI.Models.SearchResultItem)
                {
                    DgMscResults.SelectedItem = dataContext;
                    break;
                }

                depObj = VisualTreeHelper.GetParent(depObj);
            }
        }

        private void CplResultsGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (ViewModel.SelectedCplItem != null)
            {
                ViewModel.ExecuteCplCommand.Execute(ViewModel.SelectedCplItem);
            }
        }

        private void MscResultsGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (ViewModel.SelectedMscItem != null)
            {
                ViewModel.ExecuteMscCommand.Execute(ViewModel.SelectedMscItem);
            }
        }

    }
}
