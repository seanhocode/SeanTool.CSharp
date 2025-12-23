using System.ComponentModel;

namespace SeanTool.CSharp.Forms
{
    public partial class SelectForm : Form
    {
        /// <summary>
        /// 選項清單
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Dictionary<string, string> Items { get; set; }

        /// <summary>
        /// 選擇值
        /// </summary>
        public string? SelectedValue { get; private set; }

        private ComboBox SelectComboBox;
        private Button SelectBtn;

        /// <summary>
        /// 請設定Items(Dictionary<string-key, string-value>)
        /// </summary>
        /// <param name="formTitle"></param>
        public SelectForm(string formTitle = "Select")
        {
            InitializeComponent();

            this.Text = formTitle;
            // 設定視窗在父視窗/擁有者視窗的中央開啟
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void SelectProjectInfoForm_Load(object sender, EventArgs e)
        {
            SelectComboBox.DataSource = Items.Keys.ToList();

            //DDL過濾
            SelectComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            SelectComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            SelectComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            AutoSizeToContent();
        }

        /// <summary>
        /// 計算並調整視窗寬度以適應所有項目
        /// </summary>
        private void AutoSizeToContent()
        {
            if (Items == null || Items.Count == 0)
                return;

            // 計算 key 的最大顯示寬度（像素）
            int maxItemWidth = 0;
            Size size;
            foreach (var key in Items.Keys)
            {
                // 使用TextRenderer以ComboBox的字型測量字串寬度
                size = TextRenderer.MeasureText(key, SelectComboBox.Font);
                if (size.Width > maxItemWidth) maxItemWidth = size.Width;
            }

            // 額外空間：下拉按鈕/捲軸/padding
            int extra = SystemInformation.VerticalScrollBarWidth + 30;

            // 預期寬度（至少保留目前 ComboBox 的 PreferredSize）
            int windowWidth = Math.Max(maxItemWidth + extra, SelectComboBox.PreferredSize.Width);

            // 不超出工作區寬度（保留 20px margin）
            var screenWidth = Screen.FromControl(this).WorkingArea.Width;
            windowWidth = Math.Min(windowWidth, screenWidth - 20);

            // 設定Form客戶區寬度（保留原高度）
            this.ClientSize = new Size(windowWidth, this.ClientSize.Height);

            // 設定下拉寬度，確保下拉項目能完整顯示
            SelectComboBox.DropDownWidth = Math.Max(windowWidth, maxItemWidth + extra);
        }

        private void SelectBtn_Click(object sender, EventArgs e)
        {
            string? selectedValue = string.Empty;
            Items.TryGetValue(SelectComboBox.SelectedItem?.ToString() ?? string.Empty, out selectedValue);
            // 設定選擇結果
            SelectedValue = selectedValue;

            // 設定 DialogResult，讓 ShowDialog() 結束
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
