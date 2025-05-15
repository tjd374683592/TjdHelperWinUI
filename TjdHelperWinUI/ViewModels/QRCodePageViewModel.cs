using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using ZXing;
using ZXing.Windows.Compatibility;

namespace TjdHelperWinUI.ViewModels
{
    public class QRCodePageViewModel : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        /// <summary>
        /// 二维码字符串
        /// </summary>
        private string _qRCodeStr;

        public string QRCodeStr
        {
            get { return _qRCodeStr; }
            set
            {
                if (_qRCodeStr != value)
                {
                    _qRCodeStr = value;
                    OnPropertyChanged(nameof(QRCodeStr));
                }
            }
        }

        /// <summary>
        /// 二维码图片
        /// </summary>
        private BitmapImage _qRImage;

        public BitmapImage QRImage
        {
            get { return _qRImage; }
            set
            {
                if (_qRImage != value)
                {
                    _qRImage = value;
                    OnPropertyChanged(nameof(QRImage));
                }
            }
        }

        /// <summary>
        /// 二维码解码内容
        /// </summary>
        private string _qRImageContent;

        public string QRImageContent
        {
            get { return _qRImageContent; }
            set
            {
                if (_qRImageContent != value)
                {
                    _qRImageContent = value;
                    OnPropertyChanged(nameof(QRImageContent));
                }
            }
        }

        /// <summary>
        /// 二维码解码路径
        /// </summary>
        private string _decodeQRImagePath;

        public string DecodeQRImagePath
        {
            get { return _decodeQRImagePath; }
            set
            {
                if (_decodeQRImagePath != value)
                {
                    _decodeQRImagePath = value;
                    OnPropertyChanged(nameof(DecodeQRImagePath));
                }
            }
        }

        public IMessageService MsgService { get; set; }

        public ICommand CreateQRCodeCommand { get; set; }
        public ICommand ClearQRCodeCommand { get; set; }
        public ICommand OpenQRCodeFolderCommand { get; set; }
        public ICommand DecodeQRCodeCommand { get; set; }
        public ICommand ChooseQRCodePathCommand { get; set; }
        public string QRCodeSaveFilePath { get; set; }

        public QRCodePageViewModel()
        {
            MsgService = new MessageService();

            // 构建文件夹的完整路径
            QRCodeSaveFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"QRImage");
            // 判断文件夹是否存在
            if (!Directory.Exists(QRCodeSaveFilePath))
            {
                // 如果不存在，则创建文件夹
                Directory.CreateDirectory(QRCodeSaveFilePath);
            }

            CreateQRCodeCommand = new RelayCommand(CreateQRCodeCommandExecute);
            OpenQRCodeFolderCommand = new RelayCommand(OpenQRCodeFolderCommandExecute);
            ClearQRCodeCommand = new RelayCommand(ClearQRCodeCommandExecute);
            ChooseQRCodePathCommand = new RelayCommand(ChooseQRCodePathCommandExecute);
            DecodeQRCodeCommand = new RelayCommand(DecodeQRCodeCommandExecute);
        }

        private async void DecodeQRCodeCommandExecute(object obj)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(DecodeQRImagePath);
                using var stream = await file.OpenAsync(FileAccessMode.Read);

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var transform = new BitmapTransform();
                var pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                var bytes = pixelData.DetachPixelData();

                var reader = new BarcodeReader();
                var result = reader.Decode(bytes,
                    (int)decoder.PixelWidth,
                    (int)decoder.PixelHeight,
                    RGBLuminanceSource.BitmapFormat.BGR32);

                QRImageContent = result?.Text;

            }
            catch (Exception ex)
            {
                await MsgService.ShowMessageAsync("错误", "解码失败，请检查文件路径或文件格式。" + ex.Message);
            }
        }

        /// <summary>
        /// 选择二维码图片路径
        /// </summary>
        /// <param name="obj"></param>
        private async void ChooseQRCodePathCommandExecute(object obj)
        {
            string? selectedPath = await FilePickerHelper.PickSingleFilePathAsync(App.MainWindow);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                DecodeQRImagePath = selectedPath;
            }
            else
            {
                NotificationHelper.Show("通知", "操作已取消");
            }
        }

        /// <summary>
        /// 清除二维码str
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ClearQRCodeCommandExecute(object obj)
        {
            QRCodeStr = string.Empty;
            QRImage = null;

            NotificationHelper.Show("通知", "二维码已清除");
        }

        /// <summary>
        /// 打开二维码保存文件夹
        /// </summary>
        /// <param name="obj"></param>
        private void OpenQRCodeFolderCommandExecute(object obj)
        {
            Process.Start("explorer.exe", QRCodeSaveFilePath);
        }

        /// <summary>
        /// 生成QR Code
        /// </summary>
        /// <param name="obj"></param>
        private async void CreateQRCodeCommandExecute(object obj)
        {
            if (!string.IsNullOrEmpty(QRCodeStr))
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(QRCodeStr, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeAsPng = qrCode.GetGraphic(20);

                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(qrCodeAsPng.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                QRImage = bitmapImage;

                //构建文件名
                string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid() + ".png";
                // 构建完整的文件路径
                string strFinPath = Path.Combine(QRCodeSaveFilePath, fileName);
                File.WriteAllBytes(strFinPath, qrCodeAsPng);
            }
            else
            {
                //二维码字符串为空
                await MsgService.ShowMessageAsync("注意", "二维码字符串为空");
            }
        }
    }
}
