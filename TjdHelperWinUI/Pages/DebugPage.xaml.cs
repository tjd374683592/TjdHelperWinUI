using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TjdHelperWinUI.ControlHelper;
using TjdHelperWinUI.ViewModels;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DebugPage : Page
    {
        public DebugPage()
        {
            this.InitializeComponent();

            if (Content is FrameworkElement rootElement)
            {
                // �� DI �����л�ȡ ViewModel
                rootElement.DataContext = App.Services.GetService<DebugPageViewModel>();
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