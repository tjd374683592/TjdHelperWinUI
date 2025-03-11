using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Models;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class AddressHelperPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand CalcVSizeSizeCommand { get; set; }
        public ICommand CalcResultClearCommand { get; set; }

        /// <summary>
        /// 起始地址
        /// </summary>
        private string _strStartAddress;

        public string StrStartAddress
        {
            get { return _strStartAddress; }
            set
            {
                if (_strStartAddress != value)
                {
                    _strStartAddress = value;
                    OnPropertyChanged(nameof(StrStartAddress));
                }
            }
        }

        /// <summary>
        /// 结束地址
        /// </summary>
        private string _strEndAddress;

        public string StrEndAddress
        {
            get { return _strEndAddress; }
            set
            {
                if (_strEndAddress != value)
                {
                    _strEndAddress = value;
                    OnPropertyChanged(nameof(StrEndAddress));
                }
            }
        }

        /// <summary>
        /// 虚拟空间大小
        /// </summary>
        private string _strVSize;

        public string StrVSize
        {
            get { return _strVSize; }
            set
            {
                if (_strVSize != value)
                {
                    _strVSize = value;
                    OnPropertyChanged(nameof(StrVSize));
                }
            }
        }

        public AddressHelperPageViewModel()
        {

            CalcVSizeSizeCommand = new RelayCommand(CalcVSizeSizeCommandExecute);
            CalcResultClearCommand = new RelayCommand(CalcResultClearCommandExecute);
        }

        /// <summary>
        /// 根据地址计算文件大小
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CalcVSizeSizeCommandExecute(object obj)
        {
            try
            {
                // 清理输入：移除反引号、0x前缀和空白字符
                string startAddr = StrStartAddress.Replace("`", "").Replace("0x", "").Replace(" ", "").Trim().ToUpper();
                string endAddr = StrEndAddress.Replace("`", "").Replace("0x", "").Replace(" ", "").Trim().ToUpper();

                // 转换为数字
                IntPtr start = new IntPtr(Convert.ToInt64(startAddr, 16));
                IntPtr end = new IntPtr(Convert.ToInt64(endAddr, 16));

                //计算大小
                long vsize = end.ToInt64() - start.ToInt64();

                if (vsize <= 0)
                {
                    //((MainWindowViewModel)Application.Current.MainWindow.DataContext).ShowFlyOut = true;
                    //((MainWindowViewModel)Application.Current.MainWindow.DataContext).LogInfo = $"结束地址：{StrEndAddress} 必须大于起始地址：{StrStartAddress}";
                }

                StrVSize = "Size:  " + vsize + " byte    " + vsize / 1024 + "KB    " + vsize / 1024 / 1024 + "MB";
            }
            catch (Exception ex)
            {
                //((MainWindowViewModel)Application.Current.MainWindow.DataContext).ShowFlyOut = true;
                //((MainWindowViewModel)Application.Current.MainWindow.DataContext).LogInfo = "Exception: " + ex.Message;
            }
        }

        /// <summary>
        /// 清空计算结果
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CalcResultClearCommandExecute(object obj)
        {
            StrStartAddress = "";
            StrEndAddress = "";
            StrVSize = "";
        }
    }
}
