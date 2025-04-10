using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace TjdHelperWinUI.Tools
{
    public class SystemThemeHelper
    {
        public static bool IsSystemDarkTheme()
        {
            var uiSettings = new UISettings();
            Color backgroundColor = uiSettings.GetColorValue(UIColorType.Background);
            // 计算颜色亮度（简单平均）
            double brightness = (backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3.0;
            return brightness < 128; // 小于128视为深色主题
        }
    }
}
