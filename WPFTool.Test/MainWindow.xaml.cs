using SeanTool.CSharp.Net8.WPF;
using System.ComponentModel;
using System.Windows;

namespace SeanTool.CSharp.Net8.WPF.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        # region Model
        public class Address
        {
            public string City { get; set; }
            public string Street { get; set; }
            public string ZipCode { get; set; }

            public Address()
            {
                City = "Taipei";
                Street = "Sec. 4, Zhongxiao E. Rd.";
                ZipCode = "110";
            }
        }
        public enum Sex { Man, Woman }

        [DisplayName("使用者基本資訊")]
        public class Person
        {
            [DisplayName("ID(long)")]
            public long ID { get; set; }

            [DisplayName("姓名(string)")]
            public string Name { get; set; }

            [DisplayName("年紀(short)")]
            public short Age { get; set; }

            [DisplayName("性別")]
            public Sex Gender { get; set; }

            [DisplayName("電話(int)")]
            public int Phone { get; set; }

            [DisplayName("出生日期")]
            public DateTime BirthDate { get; set; }

            [DisplayName("是否啟用")]
            public bool IsEnabled { get; set; }

            [DisplayName("附加檔案路徑(*.*)")]
            [EditorPath(PathType.File)]
            public string OtherFilePath { get; set; }

            [DisplayName("照片檔案路徑(*.png)")]
            [EditorPath(PathType.File, "PNG (*.png)|*.png")]
            public string PhotoImagePath { get; set; }

            [DisplayName("照片資料夾路徑")]
            [EditorPath(PathType.Folder)]
            public string PhotoImageFolderPath { get; set; }

            public string NickName { get; set; }

            [DisplayName("身高(float)")]
            public float Height { get; set; }

            [DisplayName("體重(double)")]
            public double Weight { get; set; }

            [DisplayName("薪資(decimal)")]
            public decimal Salary { get; set; }

            [DisplayName("地址(Address)")]
            public Address HomeAddress { get; set; } = new Address();

            public Person()
            {
                ID = 123337203854775807;
                Name = "SeanHo";
                Age = 24;
                Gender = Sex.Man;
                Phone = 0900123450;
                BirthDate = new DateTime(2002, 1, 16, 1, 2, 3);
                IsEnabled = true;
                OtherFilePath = @"C:\SeanFile.txt";
                PhotoImagePath = @"C:\SeanPhoto.png";
                PhotoImageFolderPath = @"C:\";
                NickName = @"Sean";
                Height = 175.5f;
                Weight = 70.25;
                Salary = 30000.50m;
            }
        }
        # endregion
        // 1. 這是我們要編輯的測試物件
        public Person TestPerson { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // 2. 初始化資料
            TestPerson = new Person();

            // 3. 【關鍵】設定 DataContext
            // 這樣 XAML 裡的 {Binding TestPerson} 才能抓到上面的屬性
            this.DataContext = this;
        }

        private void GetModelValue(object sender, RoutedEventArgs e)
        {
            var test = TestPerson;
            bool success = true;
        }

        private void OpenModelEditorWindow(object sender, RoutedEventArgs e)
        {
            var editorWindow = new EditorModelWindow(TestPerson);

            editorWindow.ShowDialog();
        }
    }
}