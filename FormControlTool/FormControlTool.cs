
namespace Tool.FormControl
{
    public class FormControlTool
    {
        /// <summary>
        /// 打開SelectFolder視窗
        /// </summary>
        /// <param name="defaultPath">預設資料夾</param>
        /// <returns>選擇資料夾的路徑</returns>
        public static string GetSelectFolderPath(string defaultPath = "")
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "請選擇一個資料夾";
                fbd.SelectedPath = defaultPath;     // 預設開啟的資料夾

                if (fbd.ShowDialog() == DialogResult.OK)
                    return fbd.SelectedPath;

                return string.Empty;
            }
        }

        /// <summary>
        /// 打開SelectFile視窗
        /// </summary>
        /// <param name="defaultPath"></param>
        /// <returns></returns>
        public static string GetSelectFilePath(string defaultPath = "")
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "請選擇一個檔案";
                ofd.Filter = "所有檔案 (*.*)|*.*";   // 或指定檔案類型
                ofd.InitialDirectory = Path.GetDirectoryName(defaultPath); // 預設開啟的資料夾
                ofd.FileName = Path.GetFileName(defaultPath);

                if (ofd.ShowDialog() == DialogResult.OK)
                    return ofd.FileName;

                return string.Empty;
            }
        }
        /* DockStyle
            None       // 不停靠，使用 Location + Size
            Top        // 停靠到父容器上方
            Bottom     // 停靠到父容器下方
            Left       // 停靠到父容器左側
            Right      // 停靠到父容器右側
            Fill       // 填滿整個父容器
        */
    }
}
