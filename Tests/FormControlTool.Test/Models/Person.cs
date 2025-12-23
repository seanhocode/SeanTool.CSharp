using System.ComponentModel;
using Test.Data.Models;

namespace SeanTool.CSharp.Forms.Test
{

    [DisplayName("使用者基本資訊")]
    public class Person : PersonBase
    {
        [DisplayName("附加檔案路徑(*.*)")]
        [EditorPath(PathType.File)]
        public string OtherFilePath { get; set; }

        [DisplayName("照片檔案路徑(*.png)")]
        [EditorPath(PathType.File, "PNG (*.png)|*.png")]
        public string PhotoImagePath { get; set; }

        [DisplayName("照片資料夾路徑")]
        [EditorPath(PathType.Folder)]
        public string PhotoImageFolderPath { get; set; }

        public Person() : base()
        {
            OtherFilePath = @"C:\SeanFile.txt";
            PhotoImagePath = @"C:\SeanPhoto.png";
            PhotoImageFolderPath = @"C:\";
        }
    }
}
