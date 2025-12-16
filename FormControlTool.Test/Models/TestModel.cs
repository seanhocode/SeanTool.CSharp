using System.ComponentModel;

namespace SeanTool.CSharp.Net8.Forms.Test
{
    public class Address
    {
        [DisplayName("城市")]
        public string City { get; set; }

        [DisplayName("街道")]
        public string Street { get; set; }

        [DisplayName("郵遞區號")]
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
}
