using System.ComponentModel;
using System.Reflection;

namespace Tool.FormControl.Model
{
    public class PropertyEditor
    {
        public Label ShowNameLabel { get; set; }
        public Control EditControl { get; set; }
        public Type EditControlType { get; set; }
        public string PropertyName { get; set; }

        public PropertyEditor(PropertyInfo prop, object? value)
        {
            EditControlType = prop.PropertyType;
            PropertyName = prop.Name;
            DisplayNameAttribute? displayAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            string labelText = $"{displayAttr?.DisplayName ?? prop.Name}:";

            switch (prop.PropertyType) {
                //case Type type when type == typeof(string):
                //    ShowName = new Label() { Name = prop.Name };
                //    EditControl = new TextBox();
                //    break;
                default:
                    ShowNameLabel = new Label() { 
                        Text = labelText
                        , Width = TextRenderer.MeasureText(labelText, SystemFonts.DefaultFont).Width + 2
                        , TextAlign = ContentAlignment.MiddleRight
                    };
                    EditControl = new TextBox() { 
                        Text = value?.ToString() ?? string.Empty
                        , Width = TextRenderer.MeasureText(value?.ToString() ?? string.Empty, SystemFonts.DefaultFont).Width + 10
                    };
                    break;
            }
        }
    }
}