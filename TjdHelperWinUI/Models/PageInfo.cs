using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Models
{
    // 示例：PageInfo.cs
    public class PageInfo
    {
        public string Name { get; set; }  // 页面显示名称
        public Type PageType { get; set; } // 页面的实际类型（用于导航）
    }
}
