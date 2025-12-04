using System.ComponentModel;
using System.Reflection;

namespace SeanTool.CSharp.Net8.Forms
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

    public enum PathType
    {
        File,
        Folder
    }

    public class ModelEditor<T> where T : class
    {
        private readonly T _Model;
        private readonly Dictionary<PropertyInfo, Control> _ControlMap = new Dictionary<PropertyInfo, Control>();

        public ModelEditor(T model)
        {
            _Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// 產生編輯介面的主控制項 (使用 TableLayoutPanel 自動排版)
        /// </summary>
        public Control GenerateUI(bool isViewer = false)
        {
            PropertyInfo[] modelProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 使用 TableLayoutPanel 取代絕對座標，適應性更好
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = modelProperties.Length + 1, // +1 預留彈性
                AutoSize = true,
                Padding = new Padding(10)
            };

            // 設定欄位比例：標籤欄 Auto，輸入欄 100%
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            foreach (PropertyInfo prop in modelProperties)
            {
                if (!prop.CanRead || !prop.CanWrite) continue; // 跳過唯讀屬性

                // 1. 建立 Label (嘗試讀取 DisplayName)
                string labelText = prop.Name;
                DisplayNameAttribute? displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
                if (displayNameAttr != null) labelText = displayNameAttr.DisplayName;

                Label titleLabel = new Label
                {
                    Text = $"{labelText}:",
                    Anchor = AnchorStyles.Left | AnchorStyles.Top, // 對齊左上
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true,
                    Margin = new Padding(0, 6, 10, 0) // 微調垂直位置
                };

                // 2. 建立 Input Control
                Control editorControl = CreateEditorControl(prop);
                editorControl.Dock = DockStyle.Fill; // 填滿格子
                editorControl.Enabled = !isViewer; // Viewer 模式下禁用控制項

                // 3. 加入 Map 與 Layout
                _ControlMap[prop] = editorControl;
                layout.Controls.Add(titleLabel);
                layout.Controls.Add(editorControl);
            }

            return layout;
        }

        /// <summary>
        /// 將 UI 的值寫回 Model
        /// </summary>
        public void SaveValues()
        {
            foreach (KeyValuePair<PropertyInfo, Control> controlItem in _ControlMap)
            {
                PropertyInfo modelProp = controlItem.Key;
                Control control = controlItem.Value;
                object valueToSet = null;

                try
                {
                    // 取得 Nullable 的底層型別，若非 Nullable 則為原刑別
                    Type propType = Nullable.GetUnderlyingType(modelProp.PropertyType) ?? modelProp.PropertyType;

                    if (control is CheckBox checkBox) { valueToSet = checkBox.Checked; }
                    // NumericUpDown.Value 是 decimal，需轉型
                    else if (control is NumericUpDown num) { valueToSet = Convert.ChangeType(num.Value, propType); }
                    else if (control is DateTimePicker dateTimePicker) { valueToSet = dateTimePicker.Value; }
                    else if (control is ComboBox comboBox) { valueToSet = comboBox.SelectedItem; }
                    else if (control is TextBox textBox) { valueToSet = textBox.Text; }
                    // 處理複合控制項 (Path Selector)
                    else if (control is Panel panel)
                    {
                        TextBox? pathTextBox = panel.Controls[0] as TextBox;
                        valueToSet = pathTextBox?.Text ?? string.Empty;
                    }

                    // 寫入屬性
                    modelProp.SetValue(_Model, valueToSet);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"寫入屬性 {modelProp.Name} 失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private Control CreateEditorControl(PropertyInfo prop)
        {
            if(prop == null) throw new ArgumentNullException(nameof(prop));
            object? propValue = prop.GetValue(_Model);
            Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // Boolean : CheckBox
            if (propType == typeof(bool))
                return new CheckBox { Checked = (propValue as bool?) ?? false, Text = "啟用" };

            // Enum : ComboBox
            if (propType.IsEnum)
            {
                ComboBox comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                comboBox.DataSource = Enum.GetValues(propType);
                comboBox.SelectedItem = propValue ?? Enum.GetValues(propType).GetValue(0); // 預設選第一個
                return comboBox;
            }

            // Numeric : NumericUpDown
            if (propType == typeof(int) || propType == typeof(long) || propType == typeof(decimal) || propType == typeof(double) || propType == typeof(float))
            {
                NumericUpDown numericUpDown = new NumericUpDown { Minimum = decimal.MinValue, Maximum = decimal.MaxValue };

                // 處理小數位數
                if (propType == typeof(decimal))
                    numericUpDown.DecimalPlaces = 24;
                else if (propType == typeof(double))
                    numericUpDown.DecimalPlaces = 12;
                else if (propType == typeof(float))
                    numericUpDown.DecimalPlaces = 4;
                else
                    numericUpDown.DecimalPlaces = 0;

                if (propValue != null) numericUpDown.Value = Convert.ToDecimal(propValue);
                return numericUpDown;
            }

            // DateTime : DateTimePicker
            if (propType == typeof(DateTime))
            {
                DateTimePicker dataTimePicker = new DateTimePicker()
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "yyyy/MM/dd HH:mm:ss"
                }; ;
                DateTime valueDT = (propValue as DateTime?) ?? DateTime.Now;
                if (valueDT < dataTimePicker.MinDate) valueDT = dataTimePicker.MinDate;
                if (valueDT > dataTimePicker.MaxDate) valueDT = dataTimePicker.MaxDate;
                dataTimePicker.Value = valueDT;
                return dataTimePicker;
            }

            // File/Folder Path
            EditorPathAttribute? pathAttr = prop.GetCustomAttribute<EditorPathAttribute>();
            if (pathAttr != null)
                return CreatePathSelector(propValue?.ToString() ?? string.Empty, pathAttr);

            // Default : TextBox
            return new TextBox { Text = propValue?.ToString() };
        }

        // 建立 "TextBox + Button" 的複合控制項
        private Control CreatePathSelector(string initValue, EditorPathAttribute attr)
        {
            Panel panel = new Panel { Height = 30, Margin = new Padding(0) }; // 容器

            TextBox textBox = new TextBox { Text = initValue, Dock = DockStyle.Fill };
            Button selectButton = new Button { Text = "...", Width = 30, Dock = DockStyle.Right };

            selectButton.Click += (s, e) =>
            {
                if (attr.Type == PathType.Folder)
                {
                    using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    {
                        if (fbd.ShowDialog() == DialogResult.OK) textBox.Text = fbd.SelectedPath;
                    }
                }
                else
                {
                    using (OpenFileDialog ofd = new OpenFileDialog { Filter = attr.Filter })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK) textBox.Text = ofd.FileName;
                    }
                }
            };

            panel.Controls.Add(textBox);
            panel.Controls.Add(selectButton);
            return panel;
        }
    }

    public class ModelEditorForm : Form
    {
        private readonly Action _OnSave;

        public ModelEditorForm(Control contentControl, Action onSaveLogic, string title = "編輯資料")
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // 限制最大寬度，避免太寬
            MaximumSize = new Size(800, 600);
            MinimumSize = new Size(400, 200);

            _OnSave = onSaveLogic;

            // 1. 內容區塊
            GroupBox gb = new GroupBox
            {
                Text = "屬性",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10)
            };
            gb.Controls.Add(contentControl);

            // 2. 按鈕區塊 (FlowLayoutPanel 靠右排版)
            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            Button btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
            Button btnOk = new Button { Text = "確定", DialogResult = DialogResult.None }; // None: 因為我們要手動驗證與存檔

            btnOk.Click += BtnOk_Click;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOk);

            Controls.Add(gb);
            Controls.Add(btnPanel);

            // 設定表單行為
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            try
            {
                _OnSave?.Invoke(); // 觸發 SaveValues
                this.DialogResult = DialogResult.OK; // 成功才關閉
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"存檔錯誤: {ex.Message}");
            }
        }
    }
}
