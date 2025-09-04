using System.ComponentModel;
using Tool.FormControl;

namespace WorkSpace_Form
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public class Person
        {
            [DisplayName("©m¦W")]
            public string Name { get; set; } = "Sean";

            [DisplayName("¦~ÄÖ")]
            public string Age { get; set; } = "30";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Person test = new Person();
            TabControl tabControl = new TabControl() { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(FormControlTool.GetEditTabPage(test));
            this.Controls.Add(tabControl);
        }
    }
}
