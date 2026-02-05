using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Models
{
    public class CimTreeNode
    {
        public string Name { get; set; }

        public CimNodeType NodeType { get; set; }

        public object? Tag { get; set; }   // 挂 WMI / CIM 原始对象

        public ObservableCollection<CimTreeNode> Children { get; }
            = new();

        public bool HasChildren { get; set; }

        public bool IsExpanded { get; set; }
    }

    public enum CimNodeType
    {
        Namespace,
        Class,
        Property,
        Method
    }

}
