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
using CommunityToolkit.Labs.WinUI.MarkdownTextBlock;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MarkdownPage : Page
    {
        private MarkdownConfig _liveConfig;

        public MarkdownPage()
        {
            this.InitializeComponent();
            _liveConfig = new MarkdownConfig();

        }
        public MarkdownConfig LiveMarkdownConfig
        {
            get => _liveConfig;
            set => _liveConfig = value;
        }
    }
}
