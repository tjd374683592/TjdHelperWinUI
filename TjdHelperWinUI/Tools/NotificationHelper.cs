using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public static class NotificationHelper
    {
        /// <summary>
        /// 显示简单的两行文本通知。
        /// </summary>
        /// <param name="title">第一行标题</param>
        /// <param name="message">第二行正文</param>
        public static void Show(string title, string message)
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }

        /// <summary>
        /// 显示一行文本通知（只含标题）。
        /// </summary>
        /// <param name="title">通知文本</param>
        public static void Show(string title)
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }

        /// <summary>
        /// 显示自定义构造好的通知（扩展用途）。
        /// </summary>
        /// <param name="builder">一个已配置的 AppNotificationBuilder</param>
        public static void Show(AppNotificationBuilder builder)
        {
            var notification = builder.BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }

        // 示例用法
        //var builder = new AppNotificationBuilder()
        //  .AddText("警告")
        //  .AddText("你的磁盘空间即将不足！")
        //  .AddInlineImage(new Uri("file:///C:/Images/warning.png"));

        //NotificationHelper.Show(builder);
    }
}
