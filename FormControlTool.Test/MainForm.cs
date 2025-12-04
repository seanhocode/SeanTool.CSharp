using SeanTool.CSharp.Net8.Forms;
using System.ComponentModel;

namespace SeanTool.CSharp.Net8.Forms.Test
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
            Person people = new Person();
            ModelEditor<Person> editor = new ModelEditor<Person>(people);

            // 1. 產生編輯介面 (此時 _ControlMap 指向編輯控制項)
            var editUI = editor.GenerateUI(false);

            ModelEditorForm editForm = new ModelEditorForm(
                editUI,
                editor.SaveValues, // 這裡傳入的 SaveValues 會讀取目前的 _ControlMap (編輯控制項)
                "系統設定編輯"
            );

            if (editForm.ShowDialog() == DialogResult.OK)
            {
                // 2. 只有在編輯完成且存檔後，才產生檢視介面
                // 這會覆蓋 _ControlMap，但因為已經存完檔了，所以沒關係
                ModelEditor<Person> viewer = new ModelEditor<Person>(people);

                ModelEditorForm viewForm = new ModelEditorForm(
                    viewer.GenerateUI(true),
                    null,
                    "系統設定檢視"
                );
                viewForm.ShowDialog();
            }
        }
    }
}
