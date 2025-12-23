namespace SeanTool.CSharp.WPF
{
    // 用來標記字串屬性是「檔案選取」還是「資料夾選取」
    [AttributeUsage(AttributeTargets.Property)]
    public class EditorPathAttribute : Attribute
    {
        public PathType Type { get; }
        public string Filter { get; } // 給 OpenFileDialog 用

        public EditorPathAttribute(PathType type, string filter = "All files (*.*)|*.*")
        {
            Type = type;
            Filter = filter;
        }
    }
}
