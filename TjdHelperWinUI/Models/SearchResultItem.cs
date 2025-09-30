using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Models
{
    /// <summary>
    /// 封装搜索结果
    /// </summary>
    public class SearchResultItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public DateTime? DateModified { get; set; }
        public string Size { get; set; }
        public SearchResultItemType SearchResultType { get; set; }
    }

    public enum SearchResultItemType
    {
        Folder,
        File
    }
}
