using System.ComponentModel;
using SeanTool.CSharp.WPFTool.Attributes.ModelEditor;
using SeanTool.CSharp.WPFTool.Enums.ModelEditor;
using Test.Data.Models;

namespace SeanTool.CSharp.WPFTool.Test.Models
{
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

        [DisplayName("備註")]
        [EditorTextArea(4)]
        public string Remark { get; set; }

        [DisplayName("唯讀欄位(ReadOnlyAttribute)")]
        [ReadOnly(true)]
        public string LockedNote { get; set; }

        public Person() : base()
        {
            OtherFilePath = @"C:\SeanFile.txt";
            PhotoImagePath = @"C:\SeanPhoto.png";
            PhotoImageFolderPath = @"C:\";
            LockedNote = "此欄位標記 [ReadOnly(true)]，即使有 Setter 仍無法編輯";
        }
    }
}
