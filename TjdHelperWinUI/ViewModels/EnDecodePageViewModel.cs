using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

        public ICommand UrlEncodeCommand { get; set; }
        public ICommand UrlDecodeCommand { get; set; }
        public ICommand ChooseFileToBase64PathCommand { get; set; }
        public ICommand CalcBase64Command { get; set; }
        public ICommand ClearBase64Command { get; set; }
        public ICommand DecodeBase64Command { get; set; }

        public EnDecodePageViewModel()
        {
            NeedCalcFileBase64 = true; // 默认选择文件路径计算Base64

            UrlEncodeCommand = new RelayCommand(UrlEncodeCommandExecute);
            UrlDecodeCommand = new RelayCommand(UrlDecodeCommandExecute);
            ChooseFileToBase64PathCommand = new RelayCommand(async _ =>
            {
                await FileHelper.ChooseFilePathAsync(path => StrFileToBase64Path = path);
            });
            CalcBase64Command = new RelayCommand(CalcBase64CommandExecute);
            ClearBase64Command = new RelayCommand(ClearBase64CommandExecute);
            DecodeBase64Command = new RelayCommand(DecodeBase64CommandExecute);
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
