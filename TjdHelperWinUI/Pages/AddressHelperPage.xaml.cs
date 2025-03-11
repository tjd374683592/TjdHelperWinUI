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
using TjdHelperWinUI.ControlHelper;
using Microsoft.Extensions.DependencyInjection;
using TjdHelperWinUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddressHelperPage : Page
    {
        public AddressHelperPage()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // 从 DI 容器中获取 ViewModel
                rootElement.DataContext = App.Services.GetService<AddressHelperPageViewModel>();
            }
        }

        private void RichEditBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is RichEditBox richEditBox)
            {
                RichEditBoxHelper.Initialize(richEditBox);
            }
        }
    }
}
