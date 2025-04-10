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
using ColorCode;
using Windows.Globalization;
using Microsoft.UI.Xaml.Documents;

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
                // 从 DI 容器中获取 ViewModel
                rootElement.DataContext = App.Services.GetService<TestPageViewModel>();
            }



            var formatter = new RichTextBlockFormatter();

            string code = @"
[
  {
    ""Catagory"": ""Windows Performance Toolkit"",
    ""Name"": ""Windows Performance Step-by-Step Guides"",
    ""Url"": ""https://learn.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-step-by-step-guides""
  },
  {
    ""Catagory"": ""Windows Performance Toolkit"",
    ""Name"": ""WPA Exercise - UI delay problem"",
    ""Url"": ""https://learn.microsoft.com/en-us/windows-hardware/test/wpt/optimizing-performance-and-responsiveness-exercise-3""
  },
  {
    ""Catagory"": ""WinDbg"",
    ""Name"": ""user mode debug"",
    ""Url"": ""https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/getting-started-with-windbg""
  }
];
    }
}";
            formatter.FormatRichTextBlock(code, Languages.Typescript, richb);
        }

    }
}
