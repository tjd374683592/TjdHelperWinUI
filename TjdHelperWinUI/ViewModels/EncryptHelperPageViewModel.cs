using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class EncryptHelperPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 待加密字符串
        /// </summary>
        private string _strToEncrypt;

        public string StrToEncrypt
        {
            get { return _strToEncrypt; }
            set
            {
                if (_strToEncrypt != value)
                {
                    _strToEncrypt = value;
                    OnPropertyChanged(nameof(StrToEncrypt));
                }
            }
        }

        /// <summary>
        /// 加密结果
        /// </summary>
        private string _strEncryptResult;

        public string StrEncryptResult
        {
            get { return _strEncryptResult; }
            set
            {
                if (_strEncryptResult != value)
                {
                    _strEncryptResult = value;
                    OnPropertyChanged(nameof(StrEncryptResult));
                }
            }
        }

        public ICommand EncryptCommand { get; set; }
        public ICommand ClearEncryptStrAndResultCommand { get; set; }

        public EncryptHelperPageViewModel()
        {
            EncryptCommand = new RelayCommand(EncryptCommandExecute);
            ClearEncryptStrAndResultCommand = new RelayCommand(ClearEncryptStrAndResultCommandExecute);
        }

        private void ClearEncryptStrAndResultCommandExecute(object obj)
        {
            StrToEncrypt = string.Empty;
            StrEncryptResult = string.Empty;
        }

        private void EncryptCommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(StrToEncrypt))
            {
                StrEncryptResult = SHAHelper.PasswordEncryption(StrToEncrypt.Trim()).Trim();
            }
        }
    }
}
