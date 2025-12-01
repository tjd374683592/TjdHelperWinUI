using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public static class ClipboardHelper
    {
        public static async void CopyToClipboard(string text)
        {
            var data = new Windows.ApplicationModel.DataTransfer.DataPackage();
            data.SetText(text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(data);
        }
    }
}