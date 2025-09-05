using WorkSpace_Form.Model;

namespace WorkSpace_Form
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public ModelEditorTest modelEditorTest = new ModelEditorTest("Alias");

        private void Form1_Load(object sender, EventArgs e)
        {
            
            //TabControl tabControl = new TabControl() { Dock = DockStyle.Fill };
            //tabControl.TabPages.Add();
            this.Controls.Add(modelEditorTest.GetEditorGroupBox());
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //MessageBox.Show($"{modelEditorTest.Name},{modelEditorTest.Age}\r\n{modelEditorTest.PhotoImagePath}\r\n{modelEditorTest.PhotoImageFolderPath}");
            modelEditorTest.SetEditControlWedgt(250);
            //this.Controls.Remove(person.GetEditorGroupBox());
            //this.Controls.Add(person.GetEditorGroupBox());
        }
    }
}
