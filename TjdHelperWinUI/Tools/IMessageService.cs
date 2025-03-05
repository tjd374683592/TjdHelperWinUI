using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public interface IMessageService
    {
        Task ShowMessageAsync(string title, string message);
        Task<bool> ShowConfirmDialogAsync(string title, string message);
    }
}
