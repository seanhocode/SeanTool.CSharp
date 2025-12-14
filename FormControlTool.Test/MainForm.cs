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
            Button addAgeBtn = new Button();
            Button minusdAgeBtn = new Button();
            ModelEditorForm editForm = new ModelEditorForm(
                people,
                viewMode: ModelEditorViewMode.Editor
            );

            addAgeBtn.Text = "Age+1";
            addAgeBtn.Click += (s, eArgs) =>
            {
                editForm.SaveToModel();
                people.Age += 1;
                editForm.LoadFromModel();
            };
            minusdAgeBtn.Text = "Age-1";
            minusdAgeBtn.Click += (s, eArgs) =>
            {
                editForm.SaveToModel();
                people.Age -= 1;
                editForm.LoadFromModel();
            };

            editForm.AddCustomizeBtn(addAgeBtn);
            editForm.AddCustomizeBtn(minusdAgeBtn);

            if (editForm.ShowDialog() == DialogResult.OK)
            {
                editForm.ViewMode = ModelEditorViewMode.Viewer;
                editForm.ShowDialog();
            }
        }

        private void GridTestBtn_Click(object sender, EventArgs e)
        {
            List<Address> addressList = new List<Address>();

            for (int i = 1; i <= 5; i++)
            {
                addressList.Add(new Address()
                {
                    City = $"City{i}",
                    Street = $"Street{i}",
                    ZipCode = $"ZipCode{i}"
                });
            }

            Form gridForm = new Form();
            Grid<Address> grid = new Grid<Address>(addressList);
            grid.Left = 400;
            gridForm.Controls.Add(grid);

            gridForm.ShowDialog();

            addressList = new List<Address>();
            gridForm = new Form();
            for (int i = 1; i <= 50_000_000; i++)
            {
                addressList.Add(new Address()
                {
                    City = $"City{i}",
                    Street = $"Street{i}",
                    ZipCode = $"ZipCode{i}"
                });
            }
            MessageBox.Show("Large data created.");
            grid.DataSource = addressList;
            gridForm.Controls.Add(grid);
            gridForm.ShowDialog();
        }
    }
}
