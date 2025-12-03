namespace SeanTool.CSharp.Net8.Forms
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

            this.Text = formTitle;
            this.TextFormMsgLabel.Text = info;
            // 設定視窗在父視窗/擁有者視窗的中央開啟
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void TextFormSubmitBtn_Click(object sender, EventArgs e)
        {
            // 設定 DialogResult，讓 ShowDialog() 結束
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
