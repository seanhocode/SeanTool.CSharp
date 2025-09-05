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
            modelEditorTest.SetEditControlWedgt(200);
            this.Controls.Add(modelEditorTest.GetEditorGroupBox());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (modelEditorTest.OpenEditWindow())
                MessageBox.Show("OK");
            else
                MessageBox.Show("Cancel");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"{modelEditorTest.Str}\r\n{modelEditorTest.Int}\r\n{modelEditorTest.PhotoImageFolderPath}\r\n{modelEditorTest.PhotoImagePath}\r\n{modelEditorTest.NoNameTest}");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }
    }
}
