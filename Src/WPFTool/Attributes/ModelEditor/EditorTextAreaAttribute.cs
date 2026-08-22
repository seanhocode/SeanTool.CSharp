namespace SeanTool.CSharp.WPFTool.Attributes.ModelEditor
{
    // 標記字串屬性要用可垂直展開的多行 TextBox 呈現
    [AttributeUsage(AttributeTargets.Property)]
    public class EditorTextAreaAttribute : Attribute
    {
        public int MinLines { get; }

        public EditorTextAreaAttribute(int minLines = 4)
        {
            MinLines = minLines;
        }
    }
}
