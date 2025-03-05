using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        private void OnImageOpened(object sender, RoutedEventArgs e)
        {
            AnimateImage();
        }

        private void AnimateImage()
        {
            AnimationBuilder.Create()
            .Scale(1, 1.1f, duration: TimeSpan.FromMilliseconds(4000), easingMode: EasingMode.EaseOut)
            .Start(HeroImage);
        }
    }
}
