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
                LoadInstancesForClass(value);
            }
        }

        private ObservableCollection<CimTreeNode> _properties = new();
        public ObservableCollection<CimTreeNode> Properties => _properties;

        private void LoadPropertiesForClass(CimTreeNode? classNode)
        {
            _properties.Clear();

            if (classNode?.Tag is not CimClass c)
                return;

            // 根分组：Properties
            var propGroup = new CimTreeNode
            {
                Name = "Properties",
                NodeType = CimNodeType.Group,
                HasChildren = true,
                IsExpanded = true
            };

            foreach (var p in c.CimClassProperties.OrderBy(x => x.Name))
            {
                propGroup.Children.Add(new CimTreeNode
                {
                    Name = $"{p.Name} : {p.CimType}",
                    NodeType = CimNodeType.Property,
                    Tag = p
                });
            }

            // 根分组：Methods
            var methodGroup = new CimTreeNode
            {
                Name = "Methods",
                NodeType = CimNodeType.Group,
                HasChildren = true,
                IsExpanded = true
            };

            foreach (var m in c.CimClassMethods.OrderBy(x => x.Name))
            {
                methodGroup.Children.Add(new CimTreeNode
                {
                    Name = BuildMethodDisplayName(m),
                    NodeType = CimNodeType.Method,
                    Tag = m
                });
            }

            _properties.Add(propGroup);
            _properties.Add(methodGroup);
        }


        private static string BuildMethodDisplayName(CimMethodDeclaration m)
        {
            var parameters = m.Parameters
                .Select(p => $"{p.Name} : {p.CimType}");

            var paramText = string.Join(", ", parameters);

            return $"{m.Name}({paramText})";
        }


        private ObservableCollection<CimTreeNode> _instances = new();
        public ObservableCollection<CimTreeNode> Instances => _instances;

        private void LoadInstancesForClass(CimTreeNode? classNode)
        {
            _instances.Clear();

            if (classNode?.Tag is not CimClass c ||
                SelectedNamespace?.Tag is not string ns)
                return;

            using var session = CimSession.Create("localhost");

            try
            {
                var instances = session.EnumerateInstances(
                    ns,
                    c.CimSystemProperties.ClassName);

                foreach (var inst in instances)
                {
                    var instanceNode = new CimTreeNode
                    {
                        Name = BuildInstanceDisplayName(inst),
                        NodeType = CimNodeType.Instance,
                        Tag = inst,
                        HasChildren = true
                    };

                    // 👇 把 instance 的 property 直接挂成子节点，对 instance 的 property 按名字排序
                    foreach (var p in inst.CimInstanceProperties.OrderBy(x => x.Name))
                    {
                        instanceNode.Children.Add(new CimTreeNode
                        {
                            Name = $"{p.Name} : {p.Value}",
                            NodeType = CimNodeType.Property,
                            Tag = p
                        });
                    }

                    _instances.Add(instanceNode);
                }
            }
            catch
            {
                // 权限 / provider 不支持 instance
            }
        }

        private static string BuildInstanceDisplayName(CimInstance inst)
        {
            var keys = inst.CimInstanceProperties
                .Where(p => p.Flags.HasFlag(CimFlags.Key))
                .Select(p => $"{p.Name}={p.Value}");

            return keys.Any()
                ? string.Join(", ", keys)
                : inst.CimSystemProperties.ClassName;
        }


    }
}