using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;
using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace TjdHelperWinUI.Tools
{
    public static class NotificationHelper
    {
        private const string AppId = "TjdHelperWinUI"; // 用于 Unpackaged 模式
        private static readonly string IconPath =
            "file:///" + System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.scale-200.png").Replace("\\", "/");

        /// <summary>
        /// 判断当前应用是否有 Identity（即是否打包/稀疏包）。
        /// </summary>
        private static bool HasIdentity()
        {
            try
            {
                // 如果是 Unpackaged，调用会抛异常
                var dummy = Windows.ApplicationModel.Package.Current.Id;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 显示两行文本通知。
        /// </summary>
        public static void Show(string title, string message)
        {
            if (HasIdentity())
            {
                // 用 AppNotificationManager（现代 API，需要 Identity）
                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
            else
            {
                // 回退到 ToastNotificationManager（旧 API，无需 Identity）
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

                // 文本
                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(title));
                stringElements[1].AppendChild(toastXml.CreateTextNode(message));

                // 图标
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = IconPath;

                var toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
            }
        }

        /// <summary>
        /// 显示单行文本通知。
        /// </summary>
        public static void Show(string title)
        {
            if (HasIdentity())
            {
                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
            else
            {
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);

                // 文本
                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(title));

                // 图标
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = IconPath;

                var toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
            }
        }

        /// <summary>
        /// 显示自定义构造好的通知（仅 Packaged 有效）。
        /// </summary>
        public static void Show(AppNotificationBuilder builder)
        {
            if (HasIdentity())
            {
                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            else
            {
                // Unpackaged 下无法直接用 AppNotificationBuilder，给出提示
                Show("通知不支持", "当前为 Unpackaged 模式，无法使用自定义通知。");
            }
        }
    }
}