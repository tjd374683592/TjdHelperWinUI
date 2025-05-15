using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class TimeHelperPageViewModel : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public IMessageService MsgService { get; set; }

        public ICommand ConvertToTimeCommand { get; private set; }
        public ICommand ConvertToTimestampCommand { get; private set; }

        public TimeHelperPageViewModel()
        {
            MsgService = new MessageService();

            SecondsIsChecked = true;
            MillisecondsIsChecked = false;

            DateTimeOffset date = DateTimeOffset.Now.Date;
            SelectedDate = date;

            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            SelectedTime = currentTime;

            ConvertToTimeCommand = new RelayCommand(ConvertToTimeCommandExecute);
            ConvertToTimestampCommand = new RelayCommand(ConvertToTimestampCommandExecute);
        }

        /// <summary>
        /// 绑定时间转换TimeHelper User Control中的txtTime控件WaterMark属性值
        /// </summary>
        private string _txtTimeInfoWaterMark;

        public string TxtTimeInfoWaterMark
        {
            get { return _txtTimeInfoWaterMark; }
            set
            {
                if (_txtTimeInfoWaterMark != value)
                {
                    _txtTimeInfoWaterMark = value;
                    OnPropertyChanged(nameof(TxtTimeInfoWaterMark));
                }
            }
        }

        private DateTimeOffset _selectedDate;

        public DateTimeOffset SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                }
            }
        }


        private TimeSpan _selectedTime;

        public TimeSpan SelectedTime
        {
            get { return _selectedTime; }
            set
            {
                if (_selectedTime != value)
                {
                    _selectedTime = value;
                    OnPropertyChanged(nameof(SelectedTime));
                }
            }
        }

        /// <summary>
        /// Radio Button秒选中状态
        /// </summary>
        private bool _secondsIsChecked;

        public bool SecondsIsChecked
        {
            get { return _secondsIsChecked; }
            set
            {
                if (_secondsIsChecked != value)
                {
                    _secondsIsChecked = value;
                    OnPropertyChanged(nameof(SecondsIsChecked));
                }
            }
        }

        /// <summary>
        /// Radio Button毫秒选中状态
        /// </summary>
        private bool _millisecondsIsChecked;

        public bool MillisecondsIsChecked
        {
            get { return _millisecondsIsChecked; }
            set
            {
                if (_millisecondsIsChecked != value)
                {
                    _millisecondsIsChecked = value;
                    OnPropertyChanged(nameof(MillisecondsIsChecked));
                }
            }
        }

        /// <summary>
        /// 时间戳的值
        /// </summary>
        private string _timestampStr;

        public string TimestampStr
        {
            get { return _timestampStr; }
            set
            {
                if (_timestampStr != value)
                {
                    _timestampStr = value;
                    OnPropertyChanged(nameof(TimestampStr));
                }
            }
        }

        /// <summary>
        /// 时间的值
        /// </summary>
        private string _timeStr;

        public string TimeStr
        {
            get { return _timeStr; }
            set
            {
                if (_timeStr != value)
                {
                    _timeStr = value;
                    OnPropertyChanged(nameof(TimeStr));
                }
            }
        }

        /// <summary>
        /// 转换结果
        /// </summary>
        private string _timeConvertResult;

        public string TimeConvertResult
        {
            get { return _timeConvertResult; }
            set
            {
                if (_timeConvertResult != value)
                {
                    _timeConvertResult = value;
                    OnPropertyChanged(nameof(TimeConvertResult));
                }
            }
        }

        /// <summary>
        /// 时间戳转换时间
        /// </summary>
        /// <param name="parameter"></param>
        private void ConvertToTimeCommandExecute(object parameter)
        {
            if (string.IsNullOrEmpty(TimestampStr))
            {
                MsgService.ShowMessageAsync("注意", "时间戳为空");
                return;
            }

            if (TimestampStr.Length == 10)
            {
                //秒级
                MillisecondsIsChecked = false;
                SecondsIsChecked = true;
            }
            else
            {
                //毫秒级
                MillisecondsIsChecked = true;
                SecondsIsChecked = false;
            }
            string strTimeResult = TimeHelper.ConvertToTimeByTimestamp(long.Parse(TimestampStr), MillisecondsIsChecked);
            TimeSpan span = TimeSpan.FromMinutes(5);
            //5分钟前的时间戳
            TimeResultObj timeBefore = TimeHelper.GetTimeBefore(TimestampStr, span, MillisecondsIsChecked);

            //5分钟后的时间戳
            TimeResultObj timeAftere = TimeHelper.GetTimeAfter(TimestampStr, span, MillisecondsIsChecked);

            TimeConvertResult = GetFinalTimeResultStr(strTimeResult, timeBefore, timeAftere);
        }

        /// <summary>
        /// 时间转换时间戳
        /// </summary>
        /// <param name="parameter"></param>
        private void ConvertToTimestampCommandExecute(object parameter)
        {
            string timeValue = "";
            if (!string.IsNullOrEmpty(TimeStr))
            {
                //时间字符串格式校验
                bool isValidFormat = DateTime.TryParseExact(TimeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateTime);
                if (isValidFormat)
                {
                    timeValue = parsedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    MsgService.ShowMessageAsync("注意", "时间格式不正确，请使用yyyy-MM-dd HH:mm:ss");
                    return;
                }
            }
            else
            {
                // 合并 date 和 time
                DateTime combinedDateTime = SelectedDate.Date + SelectedTime;

                // 格式化成字符串
                timeValue = combinedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            long timestampNow = TimeHelper.ConvertToTimestampByTime(timeValue, MillisecondsIsChecked);
            TimeSpan span = TimeSpan.FromMinutes(5);
            //5分钟前的时间戳
            TimeResultObj timeBefore = TimeHelper.GetTimeBefore(timestampNow.ToString(), span, MillisecondsIsChecked);

            //5分钟后的时间戳
            TimeResultObj timeAftere = TimeHelper.GetTimeAfter(timestampNow.ToString(), span, MillisecondsIsChecked);

            TimeConvertResult = GetFinalTimeResultStr(timestampNow.ToString(), timeBefore, timeAftere);


        }

        /// <summary>
        /// 拼接时间转换结果字符串
        /// </summary>
        /// <param name="strTimeResult"></param>
        /// <param name="timeBefore"></param>
        /// <param name="timeAftere"></param>
        /// <returns></returns>
        private static string GetFinalTimeResultStr(string strTimeResult, TimeResultObj timeBefore, TimeResultObj timeAftere)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("转换结果：");
            sb.AppendLine(strTimeResult);
            sb.AppendLine();
            sb.AppendLine("5分钟前：");
            sb.AppendLine("Timestamp：" + timeBefore.TimestampBefore);
            sb.AppendLine("Time：" + timeBefore.TimeBefore);
            sb.AppendLine();
            sb.AppendLine("5分钟后：");
            sb.AppendLine("Timestamp：" + timeAftere.TimestampAfter);
            sb.Append("Time：" + timeAftere.TimeAfter);
            return sb.ToString();
        }

    }
}
