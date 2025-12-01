namespace FormControlTool.Forms
{
    public partial class TextForm : Form
    {
        /// <summary>
        /// 輸入的文字
        /// </summary>
        public string InputText { get { return TextFormInputTextBox.Text; } }

        public TextForm(string formTitle = "Select", string info = "PleaseEnter")
        {
            InitializeComponent();
            Text = formTitle;
            TextFormMsgLabel.Text = info;
        }

        private void TextFormSubmitBtn_Click(object sender, EventArgs e)
        {
            // 設定 DialogResult，讓 ShowDialog() 結束
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
