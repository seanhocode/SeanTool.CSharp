using System.ComponentModel;
using Tool.FormControl.Model;

namespace WorkSpace_Form.Model
{
    [DisplayName("ModelEditor測試")]
    public class ModelEditorTest : ModelEditor
    {
        [DisplayName("字串測試")]
        public string Name { get; set; }

        [DisplayName("整數測試")]
        public int Age { get; set; }

        [DisplayName("檔案路徑測試")]
        public string PhotoImagePath { get; set; }

        [DisplayName("資料夾路徑測試")]
        public string PhotoImageFolderPath { get; set; }

        public string NoNameTest { get; set; }

        public ModelEditorTest(string modelAlias = "") : base(modelAlias)
        {
            Name = "Sean";
            Age = 24;
            PhotoImagePath = @"C:\GSS\Radar\Project\GSS\GSS_RADAR-MODELS\GSS.Radar.Domain.Models";
            PhotoImageFolderPath = @"C:\GSS\Radar\Project\GSS\GSS_RADAR-MODELS\GSS.Radar.Domain.Models";
            NoNameTest = @"C:\GSS\";
        }
    }
}
