using Test.Data.Models;

namespace SeanTool.CSharp.Forms.Test
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

        # region Grid Test
        private void GridTestBtn_Click(object sender, EventArgs e)
        {
            // 產生測試資料
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

            // 產生欄位定義
            IList<GridColumnDefinition<Address>> myColumnDefinitions = addressList.GenerateDefaultColumns<Address>();
            GridColumnDefinition<Address> showDataBtn = new GridColumnDefinition<Address>();
            showDataBtn.IsButtonColumn = true;
            showDataBtn.ButtonClickAction = ShowData;
            showDataBtn.ColumnValue = "ShowData";
            showDataBtn.ColumnName = "ShowDataBtn";
            showDataBtn.HeaderText = "顯示地址";
            showDataBtn.Width = 100;
            myColumnDefinitions.Add(showDataBtn);

            // 產生Grid視窗
            Form gridForm = new Form();
            gridForm.LoadGridFormSetting();
            Grid<Address> grid = new Grid<Address>(addressList, columnDefinitions: myColumnDefinitions, editMode: GridEditMode.Editable);
            gridForm.Controls.Add(grid);

            // Show Grid
            gridForm.ShowDialog();

            // 重新產生大筆資料
            addressList = new List<Address>();
            for (int i = 1; i <= 500_000; i++)
            {
                addressList.Add(new Address()
                {
                    City = $"City{i}",
                    Street = $"Street{i}",
                    ZipCode = $"ZipCode{i}"
                });
            }
            MessageBox.Show("Large data created.");

            // Grid重新載入資料
            grid.DataSource = addressList;

            gridForm.ShowDialog();

            MessageBox.Show("Grid closed.");
        }

        private void ShowData(Address address){
            MessageBox.Show($"地址:{address.ZipCode}-{address.City}{address.Street}");
        }
        # endregion
    }
}
