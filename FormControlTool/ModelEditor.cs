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
        public TableLayoutPanel GenerateUI(bool isViewer = false)
        {
            PropertyInfo[] modelProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 使用 TableLayoutPanel 取代絕對座標
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
                if (!prop.CanRead || !prop.CanWrite) continue;      // 跳過唯讀屬性

                // 建立 Label (嘗試讀取 DisplayName)
                string labelText = prop.Name;
                DisplayNameAttribute? displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
                if (displayNameAttr != null) labelText = displayNameAttr.DisplayName;

                Label titleLabel = new Label
                {
                    Text = $"{labelText}:",
                    Anchor = AnchorStyles.Left | AnchorStyles.Top,  // 對齊左上
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true,
                    Margin = new Padding(0, 6, 10, 0)               // 微調垂直位置
                };

                Control? editorControl = CreateEditorControl(prop, isViewer);

                if(editorControl == null) continue;
                editorControl.Dock = DockStyle.Fill;

                // 加入 Map 與 Layout
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
                    if(valueToSet != null) modelProp.SetValue(_Model, valueToSet);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"寫入屬性 {modelProp.Name} 失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 建立對應屬性的編輯控制項
        /// </summary>
        /// <param name="prop"></param>
        /// <remarks>根據屬性類型建立相應的編輯控制項</remarks>
        /// <returns></returns>
        private Control? CreateEditorControl(PropertyInfo prop, bool isViewer)
        {
            if (prop == null) throw new ArgumentNullException(nameof(prop));
            object? propValue = prop.GetValue(_Model);
            Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // ============================== Type-Boolean => CheckBox ==============================
            if (propType == typeof(bool))
                return new CheckBox { Checked = (propValue as bool?) ?? false, Enabled = !isViewer };
            // ============================== Type-Boolean => CheckBox ==============================

            // ============================== Type-Enum => ComboBox =================================
            if (propType.IsEnum)
            {
                ComboBox comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !isViewer };
                Array enumValues = Enum.GetValues(propType);

                // 在 Control 還沒加入 Form 之前，確保 Items.Count 
                foreach (var value in enumValues)
                    comboBox.Items.Add(value);

                if (enumValues.Length > 0)
                {
                    int index = -1;

                    // 使用 Array.IndexOf 來找位置 (比對數值，不依賴 UI)
                    if (propValue != null)
                        index = Array.IndexOf(enumValues, propValue);

                    comboBox.SelectedIndex = (index != -1) ? index : 0;
                }

                return comboBox;
            }
            // ============================== Type-Enum => ComboBox =================================

            // ============================== Type-Numeric => NumericUpDown =========================
            if (
                propType == typeof(decimal) || propType == typeof(double) || propType == typeof(float)
                || propType == typeof(short) || propType == typeof(int) || propType == typeof(long)
            )
            {
                NumericUpDown numericUpDown = new NumericUpDown { Enabled = !isViewer };

                // 處理小數位數
                switch (propType)
                {
                    case Type type when type == typeof(decimal):
                        numericUpDown.Minimum = decimal.MinValue; 
                        numericUpDown.Maximum = decimal.MaxValue;
                        numericUpDown.DecimalPlaces = 24;
                        numericUpDown.Increment = 0.001M;
                        break;
                    case Type type when type == typeof(double):
                        numericUpDown.Minimum = decimal.MinValue;
                        numericUpDown.Maximum = decimal.MaxValue;
                        numericUpDown.DecimalPlaces = 12;
                        numericUpDown.Increment = 0.01M;
                        break;
                    case Type type when type == typeof(float):
                        numericUpDown.Minimum = decimal.MinValue;
                        numericUpDown.Maximum = decimal.MaxValue;
                        numericUpDown.DecimalPlaces = 4;
                        numericUpDown.Increment = 0.1M;
                        break;
                    case Type type when type == typeof(short):
                        numericUpDown.Minimum = short.MinValue;
                        numericUpDown.Maximum = short.MaxValue;
                        numericUpDown.DecimalPlaces = 0;
                        numericUpDown.Increment = 1M;
                        break;
                    case Type type when type == typeof(int):
                        numericUpDown.Minimum = int.MinValue;
                        numericUpDown.Maximum = int.MaxValue;
                        numericUpDown.DecimalPlaces = 0;
                        numericUpDown.Increment = 10M;
                        break;
                    case Type type when type == typeof(long):
                        numericUpDown.Minimum = long.MinValue;
                        numericUpDown.Maximum = long.MaxValue;
                        numericUpDown.DecimalPlaces = 0;
                        numericUpDown.Increment = 100M;
                        break;
                }

                if (propValue != null) numericUpDown.Value = Convert.ToDecimal(propValue);
                return numericUpDown;
            }
            // ============================== Type-Numeric => NumericUpDown =========================

            // ============================== Type-DateTime => DateTimePicker =======================
            if (propType == typeof(DateTime))
            {
                DateTimePicker dataTimePicker = new DateTimePicker()
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "yyyy/MM/dd HH:mm:ss", 
                    Enabled = !isViewer
                }
                ; ;
                DateTime valueDT = (propValue as DateTime?) ?? DateTime.Now;
                if (valueDT < dataTimePicker.MinDate) valueDT = dataTimePicker.MinDate;
                if (valueDT > dataTimePicker.MaxDate) valueDT = dataTimePicker.MaxDate;
                dataTimePicker.Value = valueDT;
                return dataTimePicker;
            }
            // ============================== Type-DateTime => DateTimePicker =======================

            // ============================== Type-String(CustomAttr) => PathSelector ===============
            EditorPathAttribute? pathAttr = prop.GetCustomAttribute<EditorPathAttribute>();
            if (pathAttr != null)
                return CreatePathSelector(propValue?.ToString() ?? string.Empty, pathAttr, isViewer);
            // ============================== Type-String(CustomAttr) => PathSelector ===============

            // ============================== Type-String => TextBox ================================
            if (propType == typeof(string))
                return new TextBox { Text = propValue?.ToString() ?? string.Empty, Enabled = !isViewer };
            // ============================== Type-String => TextBox ================================

            // ============================== Type-Class => ModelEditor =============================
            if (propType.IsClass && propType != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
            {
                Button editBtn = new Button { Text = isViewer ? $"檢視 {prop.Name}" : $"編輯 {prop.Name}", AutoSize = true };

                editBtn.Click += (s, e) =>
                {
                    try
                    {
                        // Step.1 取得子物件的值，如果是 null 則嘗試 new 一個新的
                        object? subModel = prop.GetValue(_Model);
                        if (subModel == null)
                        {
                            // 嘗試建立實體 (需有無參數建構子)
                            ConstructorInfo? ctor = propType.GetConstructor(Type.EmptyTypes);
                            if (ctor != null)
                            {
                                subModel = ctor.Invoke(null);
                                prop.SetValue(_Model, subModel); // 寫回父層，避免下次進來還是 null
                            }
                            else
                            {
                                MessageBox.Show($"無法編輯 {prop.Name}，因為它為空且沒有無參數建構子。", "錯誤");
                                return;
                            }
                        }

                        // Step.2 利用 Reflection 動態建立 ModelEditor<T>
                        Type editorSpecificType = typeof(ModelEditor<>).MakeGenericType(propType);
                        object? subEditor = Activator.CreateInstance(editorSpecificType, subModel);

                        if (subEditor == null) return;

                        // Step.3 取得Editor畫面(利用 Reflection 呼叫 GenerateUI())
                        MethodInfo? methodGenUI = editorSpecificType.GetMethod("GenerateUI");
                        TableLayoutPanel? subModelEditorUI = methodGenUI?.Invoke(subEditor, new object[] { isViewer }) as TableLayoutPanel;

                        // Step.4 取得存檔 method (利用 Reflection)
                        MethodInfo? methodSave = editorSpecificType.GetMethod("SaveValues");
                        Action subModelEditorSaveAction = () => methodSave?.Invoke(subEditor, null);

                        if (subModelEditorUI != null)
                        {
                            // Step.5 開啟新的視窗
                            using (ModelEditorForm subForm = new ModelEditorForm(subModelEditorUI, subModelEditorSaveAction, $"編輯 - {prop.Name}"))
                            {
                                if (subForm.ShowDialog() == DialogResult.OK)
                                {
                                    // 雖然 subModel 是參考型別，修改屬性會直接反應，但為了保險起見 (或處理 struct) 再 SetValue 一次
                                    prop.SetValue(_Model, subModel);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"開啟子編輯器失敗: {ex.Message}");
                    }
                };

                return editBtn;
            }
            // ============================== Type-Class => ModelEditor =============================

            return null;
        }

        /// <summary>
        /// 建立 "TextBox + Button" 複合控制項
        /// </summary>
        /// <param name="initValue"></param>
        /// <param name="attr"></param>
        /// <returns>PathSelector</returns>
        private Control CreatePathSelector(string initValue, EditorPathAttribute attr, bool isViewer)
        {
            Panel panel = new Panel { Height = 30, Margin = new Padding(0) }; // 容器

            TextBox textBox = new TextBox { Text = initValue, Dock = DockStyle.Fill, Enabled = !isViewer };
            Button selectButton = new Button { Text = "...", Width = 30, Dock = DockStyle.Right, Enabled = !isViewer };

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
        private readonly Action _SaveAction;

        public ModelEditorForm(Control contentControl, Action saveAction, string title = "編輯資料")
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // 限制最大寬度，避免太寬
            MaximumSize = new Size(800, 600);
            MinimumSize = new Size(400, 200);

            _SaveAction = saveAction;

            // Step.1 內容區塊
            GroupBox groupBox = new GroupBox
            {
                Text = "屬性",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10)
            };
            groupBox.Controls.Add(contentControl);

            // Step.2 按鈕區塊 (FlowLayoutPanel 靠右排版)
            FlowLayoutPanel btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            Button cancelBtn = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
            Button okBtn = new Button { Text = "確定", DialogResult = DialogResult.None }; // None: 因為要手動驗證與存檔

            okBtn.Click += OkBtn_Click;

            btnPanel.Controls.Add(cancelBtn);
            btnPanel.Controls.Add(okBtn);

            Controls.Add(groupBox);
            Controls.Add(btnPanel);

            AcceptButton = okBtn;
            CancelButton = cancelBtn;
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                _SaveAction?.Invoke();                  // 觸發 SaveValues
                this.DialogResult = DialogResult.OK;    // 成功才關閉
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"存檔錯誤: {ex.Message}");
            }
        }
    }
}
