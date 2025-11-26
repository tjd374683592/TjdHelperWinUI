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
using TjdHelperWinUI.Tools;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TjdHelperWinUI.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HtmlPage : Page
{
    public HtmlPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        await PreviewWebView.EnsureCoreWebView2Async();
    }

    private void HtmlInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PreviewWebView.CoreWebView2 != null)
        {
            PreviewWebView.NavigateToString(HtmlInputBox.Text);
        }
    }

    private void HtmlInputBox_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private async void HtmlInputBox_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                if (items[0] is StorageFile file && file.FileType.ToLower() == ".html")
                {
                    string text = await FileIO.ReadTextAsync(file);

                    // 更新 TextBox
                    HtmlInputBox.Text = text;

                    // WebView 显示内容
                    if (PreviewWebView.CoreWebView2 != null)
                    {
                        try
                        {
                            PreviewWebView.NavigateToString(text);
                        }
                        catch (Exception ex)
                        {
                            NotificationHelper.Show(ex.Message.ToString());
                        }
                    }
                }
            }
        }
    }
}