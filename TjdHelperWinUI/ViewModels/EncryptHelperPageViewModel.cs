using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

        /// <summary>
        /// 需要计算Base64的文件路径
        /// </summary>
        private string _strFileToBase64Path;

        public string StrFileToBase64Path
        {
            get { return _strFileToBase64Path; }
            set
            {
                if (_strFileToBase64Path != value)
                {
                    _strFileToBase64Path = value;
                    OnPropertyChanged(nameof(StrFileToBase64Path));
                }
            }
        }

        /// <summary>
        /// 需要计算Base64的字符串
        /// </summary>
        private string _strStringToBase64;

        public string StrStringToBase64
        {
            get { return _strStringToBase64; }
            set
            {
                if (_strStringToBase64 != value)
                {
                    _strStringToBase64 = value;
                    OnPropertyChanged(nameof(StrStringToBase64));
                }
            }
        }

        /// <summary>
        /// Base64文档Radiobutton选中状态
        /// </summary>
        private bool _needCalcFileBase64;

        public bool NeedCalcFileBase64
        {
            get { return _needCalcFileBase64; }
            set
            {
                if (_needCalcFileBase64 != value)
                {
                    _needCalcFileBase64 = value;
                    OnPropertyChanged(nameof(NeedCalcFileBase64));
                }
            }
        }

        /// <summary>
        /// Base64字符串Radiobutton选中状态
        /// </summary>
        private bool _needCalcStringBase64;

        public bool NeedCalcStringBase64
        {
            get { return _needCalcStringBase64; }
            set
            {
                if (_needCalcStringBase64 != value)
                {
                    _needCalcStringBase64 = value;
                    OnPropertyChanged(nameof(NeedCalcStringBase64));
                }
            }
        }

        /// <summary>
        /// Base64计算结果
        /// </summary>
        private string _strBase64Result;

        public string StrBase64Result
        {
            get { return _strBase64Result; }
            set
            {
                if (_strBase64Result != value)
                {
                    _strBase64Result = value;
                    OnPropertyChanged(nameof(StrBase64Result));
                }
            }
        }

        /// <summary>
        /// Base64解码字符串
        /// </summary>
        private string _strBase64StringToDecode;

        public string StrBase64StringToDecode
        {
            get { return _strBase64StringToDecode; }
            set
            {
                if (_strBase64StringToDecode != value)
                {
                    _strBase64StringToDecode = value;
                    OnPropertyChanged(nameof(StrBase64StringToDecode));
                }
            }
        }

        /// <summary>
        /// Base64解码结果
        /// </summary>
        private string _strBase64DecodeResult;

        public string StrBase64DecodeResult
        {
            get { return _strBase64DecodeResult; }
            set
            {
                if (_strBase64DecodeResult != value)
                {
                    _strBase64DecodeResult = value;
                    OnPropertyChanged(nameof(StrBase64DecodeResult));
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
        public ICommand ChooseFileToBase64PathCommand { get; set; }
        public ICommand CalcBase64Command { get; set; }
        public ICommand ClearBase64Command { get; set; }
        public ICommand DecodeBase64Command { get; set; }

        public EncryptHelperPageViewModel()
        {
            NeedCalcFileBase64 = true; // 默认选择文件路径计算Base64

            EncryptCommand = new RelayCommand(EncryptCommandExecute);
            ClearEncryptStrAndResultCommand = new RelayCommand(ClearEncryptStrAndResultCommandExecute);
            ChooseFilePathCommand = new RelayCommand(ChooseFilePathCommandExecute);
            CalcStrMD5HashCommand = new RelayCommand(CalcStrMD5HashCommandExecute);
            ClearMD5StrAndValueCommand = new RelayCommand(ClearMD5StrAndValueCommandExecute);
            CalcFileMD5HashCommand = new RelayCommand(CalcFileMD5HashCommandExecute);
            GenerateGUIDCommand = new RelayCommand(GenerateGUIDCommandExecute);
            ChooseFileToBase64PathCommand = new RelayCommand(ChooseFileToBase64PathCommandExecute);
            CalcBase64Command = new RelayCommand(CalcBase64CommandExecute);
            ClearBase64Command = new RelayCommand(ClearBase64CommandExecute);
            DecodeBase64Command = new RelayCommand(DecodeBase64CommandExecute);
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

        /// <summary>
        /// 选择文件路径
        /// </summary>
        /// <param name="obj"></param>
        private async void ChooseFileToBase64PathCommandExecute(object obj)
        {
            string? selectedPath = await FilePickerHelper.PickSingleFilePathAsync(App.MainWindow);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                StrFileToBase64Path = selectedPath;
            }
            else
            {
                NotificationHelper.Show("通知", "操作已取消");
            }
        }

        /// <summary>
        /// 计算Base64编码
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CalcBase64CommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(StrFileToBase64Path))
            {
                NeedCalcFileBase64 = true;
            }
            else if (!string.IsNullOrEmpty(StrStringToBase64))
            {
                NeedCalcStringBase64 = true;
            }


            if (NeedCalcFileBase64 && !string.IsNullOrEmpty(StrFileToBase64Path))
            {
                // 文件转Base64
                byte[] fileBytes = File.ReadAllBytes(StrFileToBase64Path);
                StrBase64Result = Convert.ToBase64String(fileBytes);
            }
            else if (NeedCalcStringBase64 && !string.IsNullOrEmpty(StrStringToBase64))
            {
                // 字符串转Base64
                StrBase64Result = Convert.ToBase64String(Encoding.UTF8.GetBytes(StrStringToBase64));
            }
        }

        /// <summary>
        /// 清除Base64计算结果
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ClearBase64CommandExecute(object obj)
        {
            this.StrFileToBase64Path = string.Empty;
            this.StrStringToBase64 = string.Empty;
            this.StrBase64Result = string.Empty;
            this.StrBase64StringToDecode = string.Empty;
            this.StrBase64DecodeResult = string.Empty;
        }

        /// <summary>
        /// 解码Base64
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void DecodeBase64CommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(StrBase64StringToDecode))
            {
                try
                {
                    // Base64还原成字符串
                    StrBase64DecodeResult = Encoding.UTF8.GetString(Convert.FromBase64String(StrBase64StringToDecode));
                }
                catch (Exception ex)
                {
                    NotificationHelper.Show("错误", ex.Message);
                }
            }
        }
    }
}
