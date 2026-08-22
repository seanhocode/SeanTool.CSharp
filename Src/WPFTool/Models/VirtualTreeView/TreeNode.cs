using System.Collections.ObjectModel;

namespace SeanTool.CSharp.WPFTool.Models.VirtualTreeView
{
    /// <summary>
    /// 樹的節點
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="name">節點名稱</param>
        /// <param name="children">子節點集合</param>
        /// <param name="value">節點值</param>
        public TreeNode(string name = "", IEnumerable<TreeNode>? children = null, object? value = null)
        {
            Name = name;
            Value = value;
            if (children is not null) { foreach (TreeNode child in children) { Children.Add(child); } }
        }

        /// <summary>
        /// 節點名稱
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 子節點集合
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// 節點值
        /// </summary>
        public ObservableCollection<TreeNode> Children { get; } = [];
    }
}
