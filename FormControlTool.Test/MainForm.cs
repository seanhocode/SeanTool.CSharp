using FormControlTool.Forms;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace FormControlTool.Test
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void SelectFormTestBtn_Click(object sender, EventArgs e)
        {
            SelectForm selectForm = new SelectForm("SelectFormTitle");
            Dictionary<string, string> items = new Dictionary<string, string>()
            {
                { "Select", "Select" } ,
                { "Key1", "Value1" } ,
                { "Key2", "Value2" } ,
                { "Key3", "Value3" }
            };

            selectForm.Items = items;

            if (selectForm.ShowDialog() == DialogResult.OK)
                MessageBox.Show($"Selected value : {selectForm.SelectedValue}");
            else
                MessageBox.Show("Close SelectForm, and nothing happens.");
        }

        private void TextFormTestBtn_Click(object sender, EventArgs e)
        {
            TextForm textForm = new TextForm("TextFormTitle", "Please enter value.");

            if (textForm.ShowDialog() == DialogResult.OK)
                MessageBox.Show($"Enter value : {textForm.InputText}");
            else
                MessageBox.Show("Close TextForm, and nothing happens.");
        }

        private void ModelEditorTestBtn_Click(object sender, EventArgs e)
        {
            ModelEditorTest modelEditorTest = new ModelEditorTest();
            Button customizeBtn = new Button();

            customizeBtn.Text = "Int+10";
            customizeBtn.Click += (s, ev) =>
            {
                modelEditorTest.SaveModelEditorValue();
                modelEditorTest.Int += 10;
                modelEditorTest.LoadModelEditorValue();
            };

            modelEditorTest.AddCustomizeBtn(customizeBtn);

            modelEditorTest.OpenEditWindow();
            modelEditorTest.OpenViewWindow();
        }
    }
}
