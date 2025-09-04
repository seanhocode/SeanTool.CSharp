using System.Collections.Generic;
using System.Reflection;
using Tool.FormControl.Model;

namespace Tool.FormControl
{
    public class FormControlTool
    {
        public static TabPage GetEditTabPage<T>(T model)
        {
            if (model == null) return new TabPage();
            
            TabPage editTabPage =new TabPage(){
                Name = $"Edit{model.GetType().Name}TabPage"
                , Text = $"Edit{model.GetType().Name}"
                , Dock = DockStyle.Fill
                , AutoScroll = true
            };

            editTabPage.Controls.Add(GetEditGroupBox<T>(model));

            return editTabPage;
        }

        public static GroupBox GetEditGroupBox<T>(T model)
        {
            if (model == null) return new GroupBox();

            GroupBox editGroupBox = new GroupBox()
            {
                Name = $"Edit{model.GetType().Name}GroupBox"
                , Text = $"Edit{model.GetType().Name}"
                , AutoSize = true
                , Dock = DockStyle.Fill
            };

            IList<PropertyEditor> editorList = GetPropertyEditorList<T>(model);

            foreach (PropertyEditor editor in editorList)
            {
                editGroupBox.Controls.Add(editor.ShowNameLabel);
                editGroupBox.Controls.Add(editor.EditControl);
            }

            return editGroupBox;
        }

        public static IList<PropertyEditor> GetPropertyEditorList<T>(T model)
        {
            if(model == null) return new List<PropertyEditor>();

            IList<PropertyEditor> propertyEditorList = new List<PropertyEditor>();
            PropertyInfo[] properties = model.GetType().GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                propertyEditorList.Add(new PropertyEditor(prop, prop.GetValue(model)));
            }

            SetPropertyEditorLocation(propertyEditorList);

            return propertyEditorList;
        }

        public static void SetPropertyEditorLocation(IList<PropertyEditor> propertyEditorList)
        {
            int maxLabelWidth = propertyEditorList.Max(editor => editor.ShowNameLabel.Width)
                , maxControlWidth = propertyEditorList.Max(editor => editor.EditControl.Width)
                , positionY = 30
                , initialX = 10;

            foreach (PropertyEditor editor in propertyEditorList)
            {
                editor.ShowNameLabel.Width = maxLabelWidth;
                editor.ShowNameLabel.Left = initialX;
                editor.ShowNameLabel.Top = positionY;
                editor.EditControl.Width = maxControlWidth;
                editor.EditControl.Left = initialX + 10 + maxLabelWidth;
                editor.EditControl.Top = positionY;

                positionY += 30;
            }
        }

        /* DockStyle
            None       // 不停靠，使用 Location + Size
            Top        // 停靠到父容器上方
            Bottom     // 停靠到父容器下方
            Left       // 停靠到父容器左側
            Right      // 停靠到父容器右側
            Fill       // 填滿整個父容器
        */
    }
}
