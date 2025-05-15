using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class EnDecodePageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand UrlEncodeCommand { get; set; }
        public ICommand UrlDecodeCommand { get; set; }

        /// <summary>
        /// Url
        /// </summary>
        private string _strUrl;

        public string StrUrl
        {
            get { return _strUrl; }
            set
            {
                if (_strUrl != value)
                {
                    _strUrl = value;
                    OnPropertyChanged(nameof(StrUrl));
                }
            }
        }

        public EnDecodePageViewModel()
        {
            UrlEncodeCommand = new RelayCommand(UrlEncodeCommandExecute);
            UrlDecodeCommand = new RelayCommand(UrlDecodeCommandExecute);
        }

        /// <summary>
        /// Url Encode
        /// </summary>
        /// <param name="parameter"></param>
        private void UrlEncodeCommandExecute(object parameter)
        {
            //编码
            StrUrl = HttpUtility.UrlEncode(StrUrl);
        }

        /// <summary>
        /// Url Decode
        /// </summary>
        /// <param name="parameter"></param>
        private void UrlDecodeCommandExecute(object parameter)
        {
            //解码
            StrUrl = HttpUtility.UrlDecode(StrUrl);
        }
    }
}
