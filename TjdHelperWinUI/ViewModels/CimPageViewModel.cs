using Microsoft.Management.Infrastructure;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TjdHelperWinUI.Models;

namespace TjdHelperWinUI.ViewModels
{
    public class CimPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CimPageViewModel()
        {
            LoadNamespacesRecursive();
        }

        // 根节点集合绑定到 TreeView
        public ObservableCollection<CimTreeNode> RootNodes { get; } = new();

        private void LoadNamespacesRecursive()
        {
            using var session = CimSession.Create("localhost");

            // 从 root 开始
            var rootNode = new CimTreeNode
            {
                Name = "root",
                NodeType = CimNodeType.Namespace,
                HasChildren = true,
                Tag = "root",
                IsExpanded = true // 展开一级
            };

            EnumerateNamespaces(session, "root", rootNode, 0);

            RootNodes.Add(rootNode);
        }

        private void EnumerateNamespaces(CimSession session, string ns, CimTreeNode parentNode, int depth)
        {
            try
            {
                var subNamespaces = session.EnumerateInstances(ns, "__Namespace");

                foreach (var subNs in subNamespaces)
                {
                    string name = subNs.CimInstanceProperties["Name"].Value?.ToString() ?? "";
                    string fullNs = ns + "\\" + name;

                    var node = new CimTreeNode
                    {
                        Name = name,
                        NodeType = CimNodeType.Namespace,
                        HasChildren = true,
                        Tag = fullNs,
                        IsExpanded = depth == 1 // 只展开一级子节点
                    };

                    parentNode.Children.Add(node);

                    // 递归
                    EnumerateNamespaces(session, fullNs, node, depth + 1);
                }
            }
            catch
            {
                // 权限不足的 namespace 可以忽略
            }
        }


        private CimTreeNode _selectedNamespace;
        public CimTreeNode SelectedNamespace
        {
            get => _selectedNamespace;
            set
            {
                _selectedNamespace = value;
                OnPropertyChanged(nameof(SelectedNamespace));

                // 刷新中间 class 列表
                LoadClassesForNamespace(value);
            }
        }

        private ObservableCollection<CimTreeNode> _classes = new();
        public ObservableCollection<CimTreeNode> Classes => _classes;

        private void LoadClassesForNamespace(CimTreeNode? nsNode)
        {
            _classes.Clear();
            if (nsNode == null) return;

            using var session = CimSession.Create("localhost");
            try
            {
                var classes = session.EnumerateClasses(nsNode.Tag?.ToString() ?? "", null)
                     .OrderBy(c => c.CimSystemProperties.ClassName);
                foreach (var c in classes)
                {
                    _classes.Add(new CimTreeNode
                    {
                        Name = c.CimSystemProperties.ClassName,
                        NodeType = CimNodeType.Class,
                        Tag = c
                    });
                }
            }
            catch
            {
                // 权限不足或异常忽略
            }
        }


        private CimTreeNode? _selectedClass;
        public CimTreeNode? SelectedClass
        {
            get => _selectedClass;
            set
            {
                _selectedClass = value;
                OnPropertyChanged(nameof(SelectedClass));
                LoadPropertiesForClass(value);
            }
        }

        private ObservableCollection<CimTreeNode> _properties = new();
        public ObservableCollection<CimTreeNode> Properties => _properties;

        private void LoadPropertiesForClass(CimTreeNode? classNode)
        {
            _properties.Clear();
            if (classNode?.Tag is CimClass c)
            {
                foreach (var p in c.CimClassProperties)
                {
                    _properties.Add(new CimTreeNode
                    {
                        Name = $"{p.Name} : {p.CimType}",
                        NodeType = CimNodeType.Property,
                        Tag = p
                    });
                }
            }
        }
    }
}
