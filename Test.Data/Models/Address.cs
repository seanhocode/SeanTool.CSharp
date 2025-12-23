namespace Test.Data.Models
{
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
}
