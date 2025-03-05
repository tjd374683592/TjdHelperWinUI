using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public partial class TestPageViewModel: INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public TestPageViewModel()
        {
            MessageService = new MessageService();

            ClearJsonCommand = new RelayCommand(ClearJsonCommandExecute);

            ShowMessageCommand = new RelayCommand(async _ => await MessageService.ShowMessageAsync("提示", "这是一个 MVVM 消息框"));
            ShowConfirmCommand = new RelayCommand(async _ =>
            {
                bool confirmed = await MessageService.ShowConfirmDialogAsync("确认", "你确定要执行这个操作吗？");
                if (confirmed)
                {
                    await MessageService.ShowMessageAsync("成功", "操作已执行");
                }
            });
        }

        public IMessageService MessageService { get; set; }


        private void ClearJsonCommandExecute(object obj)
        {

        }

        public ICommand ClearJsonCommand { get; set; }

        public ICommand ShowMessageCommand { get; }
        public ICommand ShowConfirmCommand { get; }
    }
}
