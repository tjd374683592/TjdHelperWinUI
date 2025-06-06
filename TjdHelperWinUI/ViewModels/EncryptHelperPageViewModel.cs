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

        /// <summary>
        /// 文件路径
        /// </summary>
        private string _strMD5FilePath;

        public string StrMD5FilePath
        {
            get { return _strMD5FilePath; }
            set
            {
                if (_strMD5FilePath != value)
                {
                    _strMD5FilePath = value;
                    OnPropertyChanged(nameof(StrMD5FilePath));
                }
            }
        }

        /// <summary>
        /// 待计算MD5字符串
        /// </summary>
        private string _strToCalcMD5;

        public string StrToCalcMD5
        {
            get { return _strToCalcMD5; }
            set
            {
                if (_strToCalcMD5 != value)
                {
                    _strToCalcMD5 = value;
                    OnPropertyChanged(nameof(StrToCalcMD5));
                }
            }
        }

        /// <summary>
        /// MD5结果
        /// </summary>
        private string _strMD5Result;

        public string StrMD5Result
        {
            get { return _strMD5Result; }
            set
            {
                if (_strMD5Result != value)
                {
                    _strMD5Result = value;
                    OnPropertyChanged(nameof(StrMD5Result));
                }
            }
        }

        /// <summary>
        /// GUID结果
        /// </summary>
        private string _strGUID;

        public string StrGUID
        {
            get { return _strGUID; }
            set
            {
                if (_strGUID != value)
                {
                    _strGUID = value;
                    OnPropertyChanged(nameof(StrGUID));
                }
            }
        }

        public ICommand EncryptCommand { get; set; }
        public ICommand ClearEncryptStrAndResultCommand { get; set; }
        public ICommand ChooseFilePathCommand { get; set; }
        public ICommand CalcStrMD5HashCommand { get; set; }
        public ICommand ClearMD5StrAndValueCommand { get; set; }
        public ICommand CalcFileMD5HashCommand { get; set; }
        public ICommand GenerateGUIDCommand { get; set; }

        public EncryptHelperPageViewModel()
        {
            EncryptCommand = new RelayCommand(EncryptCommandExecute);
            ClearEncryptStrAndResultCommand = new RelayCommand(ClearEncryptStrAndResultCommandExecute);
            ChooseFilePathCommand = new RelayCommand(ChooseFilePathCommandExecute);
            CalcStrMD5HashCommand = new RelayCommand(CalcStrMD5HashCommandExecute);
            ClearMD5StrAndValueCommand = new RelayCommand(ClearMD5StrAndValueCommandExecute);
            CalcFileMD5HashCommand = new RelayCommand(CalcFileMD5HashCommandExecute);
            GenerateGUIDCommand = new RelayCommand(GenerateGUIDCommandExecute);
        }

        private void GenerateGUIDCommandExecute(object obj)
        {
            StrGUID = Guid.NewGuid().ToString();
        }

        private async void CalcStrMD5HashCommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(StrToCalcMD5))
            {
                StrMD5Result = MD5Helper.GetStringMD5(StrToCalcMD5);
            }
            else
            {
                NotificationHelper.Show("注意", "待计算MD5的字符串为空");
            }
        }

        private void CalcFileMD5HashCommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(StrMD5FilePath))
            {
                StrMD5Result = MD5Helper.CalcFileMD5(StrMD5FilePath);
            }
            else
            {
                NotificationHelper.Show("Alert", "文件路径为空");
            }
        }

        private void ClearMD5StrAndValueCommandExecute(object obj)
        {
            StrMD5FilePath = string.Empty;
            StrMD5Result = string.Empty;
            StrToCalcMD5 = string.Empty;
        }

        #region 选择文件路径命令执行
        /// <summary>
        /// 选择文件路径命令执行
        /// </summary>
        /// <param name="obj"></param>
        private async void ChooseFilePathCommandExecute(object obj)
        {
            string? selectedPath = await FilePickerHelper.PickSingleFilePathAsync(App.MainWindow);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                StrMD5FilePath = selectedPath;
            }
            else
            {
                NotificationHelper.Show("通知", "操作已取消");
            }
        }
        #endregion

        #region 清空SHA加密字符串和结果
        /// <summary>
        /// 清空SHA加密字符串和结果
        /// </summary>
        /// <param name="obj"></param>
        private void ClearEncryptStrAndResultCommandExecute(object obj)
        {
            StrToEncrypt = string.Empty;
            StrEncryptResult = string.Empty;
        }
        #endregion

        #region SHA加密执行
        /// <summary>
        /// SHA加密执行
        /// </summary>
        /// <param name="obj"></param>
        private void EncryptCommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(StrToEncrypt))
            {
                StrEncryptResult = SHAHelper.PasswordEncryption(StrToEncrypt.Trim()).Trim();
            }
        } 
        #endregion
    }
}
