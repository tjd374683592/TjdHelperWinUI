﻿using Newtonsoft.Json;
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
    public class WinErrorCodeViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand CheckWinErrorDetailsCommand { get; set; }
        public ICommand WinErrorAndDetailsClearCommand { get; set; }

        /// <summary>
        /// WinError Code字符串
        /// </summary>
        private string _strWinErrorCode;

        public string StrWinErrorCode
        {
            get { return _strWinErrorCode; }
            set
            {
                if (_strWinErrorCode != value)
                {
                    _strWinErrorCode = value;
                    OnPropertyChanged(nameof(StrWinErrorCode));
                }
            }
        }

        /// <summary>
        /// WinError Details字符串
        /// </summary>
        private string _strWinErrorCodeDetails;

        public string StrWinErrorCodeDetails
        {
            get { return _strWinErrorCodeDetails; }
            set
            {
                if (_strWinErrorCodeDetails != value)
                {
                    _strWinErrorCodeDetails = value;
                    OnPropertyChanged(nameof(StrWinErrorCodeDetails));
                }
            }
        }
        public WinErrorCodeViewModel()
        {
            CheckWinErrorDetailsCommand = new RelayCommand(CheckWinErrorDetailsCommandExecute);
            WinErrorAndDetailsClearCommand = new RelayCommand(WinErrorAndDetailsClearCommandExecute);
        }

        /// <summary>
        /// 查询Win Error Code详情
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CheckWinErrorDetailsCommandExecute(object obj)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(currentPath, @"Resources\Tools\Err_6.4.5.exe");
            string errorCodesJsonFilePath = Path.Combine(currentPath, @"Resources\Tools\errorCodes.json");

            string arguments = string.IsNullOrEmpty(StrWinErrorCode) ? "" : StrWinErrorCode.Trim();  // 传递的参数

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true, // 重定向标准输出
                    RedirectStandardError = true,  // 重定向错误输出
                    UseShellExecute = false, // 不使用shell执行
                    CreateNoWindow = true   // 不创建窗口
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    // 读取标准输出
                    string output = process.StandardOutput.ReadToEnd();
                    // 读取错误输出
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();  // 等待进程结束
                    int exitCode = process.ExitCode;  // 获取返回的退出代码


                    StringBuilder sb = new StringBuilder();
                    // 读取 JSON 文件 -> 输出中文error info
                    string jsonContent = File.ReadAllText(errorCodesJsonFilePath);
                    // 解析 JSON 数据
                    List<WinErrorInfo> errorList = JsonConvert.DeserializeObject<List<WinErrorInfo>>(jsonContent);
                    // 查找对应的错误信息
                    WinErrorInfo errorJsonInfo = new WinErrorInfo();
                    //null的时候赋值为0
                    StrWinErrorCode = string.IsNullOrEmpty(StrWinErrorCode) ? "0" : StrWinErrorCode.Trim();

                    //判断用户输入的是10进制还是16进制
                    if (DecimalHelper.IsDecimal(StrWinErrorCode))
                    {
                        errorJsonInfo = errorList.FirstOrDefault(e => e.ErrorCode == Convert.ToInt32(StrWinErrorCode));
                    }
                    else if (DecimalHelper.IsHexadecimal(StrWinErrorCode))
                    {
                        errorJsonInfo = errorList.FirstOrDefault(e => Convert.ToInt32(e.HexCode, 16) == Convert.ToInt32(StrWinErrorCode, 16));
                    }


                    if (errorJsonInfo != null)
                    {
                        sb.AppendLine("Error Code:" + errorJsonInfo.ErrorCode);
                        sb.AppendLine("Error HexCode:" + errorJsonInfo.HexCode);
                        sb.AppendLine("Error ErrorName:" + errorJsonInfo.ErrorName);
                        sb.AppendLine("Error Description:" + errorJsonInfo.Description + "\r\n\r\n");
                    }

                    //拼装ErrTool输出的内容
                    sb.AppendLine("Output:");
                    sb.AppendLine(output);
                    sb.AppendLine("Error:");
                    sb.AppendLine(error);

                    StrWinErrorCodeDetails = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                //((MainWindowViewModel)Application.Current.MainWindow.DataContext).ShowFlyOut = true;
                //((MainWindowViewModel)Application.Current.MainWindow.DataContext).LogInfo = "Exception: " + ex.Message;
            }
        }

        /// <summary>
        /// 清空WinError Code和WinError Details
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void WinErrorAndDetailsClearCommandExecute(object obj)
        {
            StrWinErrorCode = "";
            StrWinErrorCodeDetails = "";
        }
    }
}
