namespace SeanTool.CSharp.WPFTool.Models.Filter
{
    /// <summary>
    /// 樹狀資料篩選器
    /// </summary>
    public static class TreeFilterQuery
    {
        /// <summary>
        /// 篩選樹狀資料
        /// </summary>
        /// <param name="source">欲篩選的樹狀資料</param>
        /// <param name="childrenSelector">取得節點子項目的函式</param>
        /// <param name="nodePredicate">判斷節點是否符合篩選條件的函式</param>
        /// <param name="createNode">建立篩選結果節點的函式</param>
        /// <returns>篩選後的樹狀資料</returns>
        /// <remarks>
        /// <para>1. 符合條件的節點會保留完整子樹</para>
        /// <para>2. 不符合條件但包含符合條件子節點的祖先節點會保留</para>
        /// <para>3. 篩選結果會透過 createNode 建立新節點，不修改原始資料</para>
        /// </remarks>
        public static IReadOnlyList<T> Apply<T>(
            IEnumerable<T> source,
            Func<T, IEnumerable<T>> childrenSelector,
            Func<T, bool> nodePredicate,
            Func<T, IEnumerable<T>, T> createNode)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(childrenSelector);
            ArgumentNullException.ThrowIfNull(nodePredicate);
            ArgumentNullException.ThrowIfNull(createNode);

            return source
                .Select(node => FilterNode(node, childrenSelector, nodePredicate, createNode))
                .OfType<T>()
                .ToArray();
        }

        /// <summary>
        /// 遞迴篩選單一節點及其子節點
        /// </summary>
        /// <param name="node">目前處理的節點</param>
        /// <param name="childrenSelector">取得節點子項目的函式</param>
        /// <param name="nodePredicate">判斷節點是否符合篩選條件的函式</param>
        /// <param name="createNode">建立篩選結果節點的函式</param>
        /// <returns>篩選後的節點；若節點及其子節點皆不符合條件則回傳 null</returns>
        private static T? FilterNode<T>(
            T node,
            Func<T, IEnumerable<T>> childrenSelector,
            Func<T, bool> nodePredicate,
            Func<T, IEnumerable<T>, T> createNode)
            where T : class
        {
            //如果節點符合條件，則保留整個子樹
            bool matches = nodePredicate(node);
            if (matches) { return createNode(node, CloneChildren(node, childrenSelector, createNode)); }

            //節點不符合條件，則回傳符合條件的子節點(遞迴篩選子節點)
            T[] children = childrenSelector(node)
                .Select(child => FilterNode(child, childrenSelector, nodePredicate, createNode))
                .OfType<T>()
                .ToArray();

            return children.Length > 0
                ? createNode(node, children)
                : null;
        }

        /// <summary>
        /// 複製節點的完整子樹
        /// </summary>
        /// <param name="node">目前處理的節點</param>
        /// <param name="childrenSelector">取得節點子項目的函式</param>
        /// <param name="createNode">建立複製節點的函式</param>
        /// <returns>複製後的子節點集合</returns>
        private static IEnumerable<T> CloneChildren<T>(
            T node,
            Func<T, IEnumerable<T>> childrenSelector,
            Func<T, IEnumerable<T>, T> createNode)
            where T : class
        {
            return childrenSelector(node)
                .Select(child => createNode(child, CloneChildren(child, childrenSelector, createNode)))
                .ToArray();
        }
    }
}
