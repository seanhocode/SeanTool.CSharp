using System.ComponentModel;
using System.Reflection;

namespace Tool.FormControl.Model
{
    # region ModelEditor
    /// <summary>
    /// 繼承此Editor後可取得編輯Model的GroupBox
    /// </summary>
    /// <remarks></remarks>
    public abstract class ModelEditor
    {
        /// <summary>
        /// 子Model的PropertyInfo
        /// </summary>
        private PropertyInfo[] PropertyInfoList { get; set; }
        /// <summary>
        /// 預設間隔X
        /// </summary>
        private readonly int DefaultX = 10;
        /// <summary>
        /// 預設間隔Y
        /// </summary>
        private readonly int DefaultY = 25;
        private int? DefaultEditControlWidth {  get; set; }
        /// <summary>
        /// 子Model辨識別名
        /// </summary>
        private string ModelAlias { get; set; }

        /// <summary>
        /// 初始化ModelEditor
        /// </summary>
        /// <param name="modelAlias">子Model辨識別名</param>
        public ModelEditor(string modelAlias = "")
        {
            //GetProperties只會抓public屬性
            PropertyInfoList = this.GetType().GetProperties();
            ModelAlias = modelAlias;
        }

        /// <summary>
        /// 取得GroupBox
        /// </summary>
        /// <remarks>回傳前先重新Load Editor的值</remarks>
        /// <returns>編輯Model畫面的GroupBox</returns>
        public GroupBox GetEditorGroupBox()
        {
            List<PropertyEditor> propertyEditorList = GenNewPropertyEditorList();
            LoadModelEditorValue(propertyEditorList);
            return GenNewEditorGroupBox(propertyEditorList);
        }

        /// <summary>
        /// 將子Model上的值帶入EditControl
        /// </summary>
        public void LoadModelEditorValue(List<PropertyEditor> propertyEditorList)
        {
            foreach (PropertyInfo propInfo in PropertyInfoList)
            {
                Control? propEditor = GetEditControl(propertyEditorList, propInfo.Name);
                object? value = propInfo.GetValue(this);

                if (value != null && propEditor != null)
                {
                    switch (propInfo.PropertyType)
                    {
                        case Type type when type == typeof(int):
                            ((NumericUpDown)propEditor).Value = int.Parse(value?.ToString() ?? "0");
                            break;
                        default:
                            propEditor.Text = value?.ToString() ?? string.Empty;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 將EditControl的值寫回子Model
        /// </summary>
        public void SaveModelEditorValue(List<PropertyEditor> propertyEditorList)
        {
            foreach (PropertyInfo propInfo in PropertyInfoList)
            {
                Control? propEditor = GetEditControl(propertyEditorList, propInfo.Name);

                if (propEditor != null)
                {
                    switch (propInfo.PropertyType)
                    {
                        case Type type when type == typeof(int):
                            propInfo.SetValue(this, Convert.ToInt32(((NumericUpDown)propEditor).Value));
                            break;
                        default:
                            propInfo.SetValue(this, propEditor.Text);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 設定EditControl的長度
        /// </summary>
        /// <param name="Width">EditControl的長度</param>
        public void SetEditControlWedgt(int Width)
        {
            DefaultEditControlWidth = Width;
        }

        /// <summary>
        /// 直接開新視窗
        /// </summary>
        public bool OpenEditWindow()
        {
            DialogResult result = new ModelEditorForm(GetEditorGroupBox()).ShowDialog();
            if (result == DialogResult.OK) MessageBox.Show("確定不代表存檔!");
            return result == DialogResult.OK; // 點按鈕回傳 true，直接 X 回傳 false
        }

        /// <summary>
        /// 根據PropertyName取得EditControl
        /// </summary>
        /// <param name="propName">PropertyName</param>
        /// <returns>EditControl</returns>
        private Control? GetEditControl(List<PropertyEditor> propertyEditorList, string propName)
        {
            return propertyEditorList
                    .Where(editor => editor.PropertyName == propName)
                    .Select(editor => editor.EditControl)
                    .FirstOrDefault();
        }

        /// <summary>
        /// 產生編輯Model畫面的GroupBox
        /// </summary>
        /// <remarks>
        /// Text預設為GetType().Name，子Model有設定屬性[DisplayName]則會顯示DisplayName，
        /// 且如果有傳入子Model辨識別名則在後面加上.[子Model辨識別名]。
        /// Name預設為Edit[GetType().Name](.[子Model辨識別名])GroupBox
        /// </remarks>
        private GroupBox GenNewEditorGroupBox(List<PropertyEditor> propertyEditorList)
        {
            
            Button saveButton = new Button() { Text = "Save" };
            string modelName = $"{this.GetType().Name}", groupBoxTitle = string.Empty;
            DisplayNameAttribute? displayAttr = this.GetType().GetCustomAttribute<DisplayNameAttribute>();

            groupBoxTitle = string.IsNullOrEmpty(displayAttr?.DisplayName) ? modelName : displayAttr.DisplayName;

            if (!string.IsNullOrEmpty(ModelAlias))
            {
                modelName += $".{ModelAlias}";
                groupBoxTitle += $".{ModelAlias}";
            }

            GroupBox editorGroupBox = new GroupBox()
            {
                Name = $"Edit{modelName}GroupBox",
                Text = groupBoxTitle,
                //Dock = DockStyle.Fill,
                AutoSize = true
            };

            //Step.1 將PropertyEditor放入GroupBox
            if (propertyEditorList != null)
            {
                foreach (PropertyEditor editor in propertyEditorList)
                {
                    editorGroupBox.Controls.Add(editor.ShowNameLabel);
                    editorGroupBox.Controls.Add(editor.EditControl);
                    if (editor.SelectButton != null)
                        editorGroupBox.Controls.Add(editor.SelectButton);
                }
            }

            //Step.2 將saveButton放入GroupBox
            saveButton.Top = (propertyEditorList ?? new List<PropertyEditor>()).Max(editor => editor.ShowNameLabel.Top) + DefaultY;
            saveButton.Click += (sender, e) =>
            {
                SaveModelEditorValue(propertyEditorList);
                MessageBox.Show("Saved");
            };

            editorGroupBox.Controls.Add(saveButton);

            return editorGroupBox;
        }

        /// <summary>
        /// 產生PropertyEditorList
        /// </summary>
        private List<PropertyEditor> GenNewPropertyEditorList()
        {
            List<PropertyEditor> propertyEditorList = new List<PropertyEditor>();

            if (PropertyInfoList == null)
                PropertyInfoList = this.GetType().GetProperties();

            foreach (PropertyInfo prop in PropertyInfoList)
            {
                propertyEditorList.Add(new PropertyEditor(prop));
            }

            SetPropertyEditorLocation(propertyEditorList);

            return propertyEditorList;
        }

        /// <summary>
        /// 設定PropertyEditor的長寬、位置
        /// </summary>
        /// <remarks>將所有欄位名稱長度設為最長的欄位名稱長度</remarks>
        private void SetPropertyEditorLocation(List<PropertyEditor> propertyEditorList)
        {
            int maxLabelWidth = propertyEditorList.Max(editor => editor.ShowNameLabel.Width)
                , positionY = DefaultY
                , initialX = DefaultX;

            foreach (PropertyEditor editor in propertyEditorList)
            {
                editor.ShowNameLabel.Width = maxLabelWidth;
                editor.ShowNameLabel.Left = initialX;
                editor.ShowNameLabel.Top = positionY;
                if(DefaultEditControlWidth != null && DefaultEditControlWidth > 0)
                    editor.EditControl.Width = DefaultEditControlWidth.Value;
                editor.EditControl.Left = initialX + DefaultX + maxLabelWidth;
                editor.EditControl.Top = positionY;
                if (editor.SelectButton != null)
                {
                    editor.SelectButton.Left = editor.EditControl.Left + editor.EditControl.Width + DefaultX;
                    editor.SelectButton.Top = positionY;
                }

                positionY += DefaultY;
            }
        }
    }
    #endregion

    #region PropertyEditor
    /// <summary>
    /// Property編輯控制項
    /// </summary>
    public class PropertyEditor
    {
        /// <summary>
        /// 欄位名稱
        /// </summary>
        /// <remarks>預設PropertyInfo.Name，Property有設定屬性[DisplayName]則會顯示DisplayName</remarks>
        public Label ShowNameLabel { get; set; }
        /// <summary>
        /// 編輯Property的Control
        /// </summary>
        public Control EditControl { get; set; }
        /// <summary>
        /// Property名稱
        /// </summary>
        /// <remarks>存PropertyInfo.Name</remarks>
        public string PropertyName { get; set; }
        /// <summary>
        /// 選擇按鈕
        /// </summary>
        /// <remarks>PropertyInfo.Name結尾為Path:檔案、FolderPath:資料夾</remarks>
        public Button? SelectButton { get; set; }
        /// <summary>
        /// 選擇按鈕預設長度
        /// </summary>
        private int SelectButtonDefaultWidth = 90;
        /// <summary>
        /// Label長度修正
        /// </summary>
        private double OffsetLabelWidth = 1.2f;
        /// <summary>
        /// 預設EditControl長度
        /// </summary>
        private int DefaultControlWidth = 90;

        /// <summary>
        /// 根據PropertyInfo初始化PropertyEditor
        /// </summary>
        /// <param name="prop">PropertyInfo</param>
        public PropertyEditor(PropertyInfo prop)
        {
            PropertyName = prop.Name;
            DisplayNameAttribute? displayAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            string labelText = $"{displayAttr?.DisplayName ?? prop.Name}:";
            double labelWidth = TextRenderer.MeasureText(labelText, SystemFonts.DefaultFont).Width;

            labelWidth *= OffsetLabelWidth;

            //Step.1 設定ShowNameLabel
            ShowNameLabel = new Label()
            {
                Text = labelText,
                //計算文字在指定字型下的顯示大小
                Width = (int)double.Parse(labelWidth.ToString()),
                TextAlign = ContentAlignment.MiddleRight
            };

            //Step.2 根據Type設定EditControl
            switch (prop.PropertyType)
            {
                case Type type when type == typeof(int):
                    EditControl = new NumericUpDown()
                    {
                        Maximum = int.MaxValue
                        ,
                        Minimum = int.MinValue
                        ,
                        Width = DefaultControlWidth
                    };
                    break;
                default:
                    EditControl = new TextBox()
                    {
                        Width = DefaultControlWidth
                    };
                    break;
            }

            //Step.3 Path結尾的Property的設定SelectButton
            if (prop.PropertyType == typeof(string))
            {
                if (PropertyName.EndsWith("FolderPath"))
                {
                    SelectButton = new Button() { Text = "SelectFolder", Width = SelectButtonDefaultWidth };
                    SelectButton.Click += (sender, e) =>
                    {
                        EditControl.Text = FormControlTool.GetSelectFolderPath(EditControl.Text);
                    };
                }
                else if (PropertyName.EndsWith("Path"))
                {
                    SelectButton = new Button() { Text = "SelectFile", Width = SelectButtonDefaultWidth };
                    SelectButton.Click += (sender, e) =>
                    {
                        EditControl.Text = FormControlTool.GetSelectFilePath(EditControl.Text);
                    };
                }
            }
        }
    }

    #endregion

    #region ModelEditorForm
    /// <summary>
    /// ModelEditor編輯視窗
    /// </summary>
    public class ModelEditorForm : Form
    {
        public ModelEditorForm(GroupBox editorGroupBox)
        {
            this.Text = "ModelEditor";
            this.AutoSize = true;

            Button confirmBtn = new Button
            {
                Text = "確定",
                Dock = DockStyle.Bottom
            };

            // 點按鈕時關閉視窗，並回傳 DialogResult.OK
            confirmBtn.Click += (s, e) => this.DialogResult = DialogResult.OK;

            Controls.Add(editorGroupBox);
            Controls.Add(confirmBtn);

            // X 關閉按鈕預設會回傳 DialogResult.Cancel
            this.CancelButton = confirmBtn; // 可選，讓 Esc 也觸發取消
        }
    }
    #endregion
}