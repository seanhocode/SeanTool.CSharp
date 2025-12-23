using System.ComponentModel;
using Test.Data.Enums;

namespace Test.Data.Models
{
    [DisplayName("使用者基本資訊")]
    public class PersonBase
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

        public string NickName { get; set; }

        [DisplayName("身高(float)")]
        public float Height { get; set; }

        [DisplayName("體重(double)")]
        public double Weight { get; set; }

        [DisplayName("薪資(decimal)")]
        public decimal Salary { get; set; }

        [DisplayName("地址(Address)")]
        public Address HomeAddress { get; set; } = new Address();

        public PersonBase()
        {
            ID = 123337203854775807;
            Name = "SeanHo";
            Age = 24;
            Gender = Sex.Man;
            Phone = 0900123450;
            BirthDate = new DateTime(2002, 1, 16, 1, 2, 3);
            IsEnabled = true;
            NickName = @"Sean";
            Height = 175.5f;
            Weight = 70.25;
            Salary = 30000.50m;
        }
    }
}
