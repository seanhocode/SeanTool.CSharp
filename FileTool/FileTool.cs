using System.Diagnostics;

namespace FileTool
{
    public class FileTool
    {
        public static string ThisExePath = Process.GetCurrentProcess().MainModule!.FileName;
        public static string ThisExeDir = Path.GetDirectoryName(ThisExePath)!;

        /// <summary>
        /// 取得指定資料夾內所有檔案的完整路徑清單
        /// </summary>
        /// <param name="folderPath">資料夾完整路徑</param>
        /// <param name="isSearchSubFolder">是否往下撈子資料夾檔案</param>
        /// <param name="subFolderDepth">子資料夾層數</param>
        /// <returns>該資料夾底下的所有檔案</returns>
        public static List<string> GetAllFileInFolder(string folderPath, bool isSearchSubFolder = false, int subFolderDepth = -1)
        {
            return GetAllFileInFolder(folderPath, 0, isSearchSubFolder, subFolderDepth);
        }

        /// <summary>
        /// 檢查資料夾是否存在
        /// </summary>
        /// <param name="folderPath">資料夾完整路徑</param>
        /// <param name="isAutoCreate">資料夾不存在時是否自動建立</param>
        /// <returns>結束後資料夾是否存在</returns>
        /// <exception cref="ArgumentNullException">folderPath為null或空白字串時拋出</exception>
        public static bool CheckFolderExist(string folderPath, bool isAutoCreate = false)
        {

            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentNullException(nameof(folderPath), "資料夾路徑不可為空");

            if (Directory.Exists(folderPath))
                return true;

            if (isAutoCreate)
            {
                Directory.CreateDirectory(folderPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 檢查檔案是否存在
        /// </summary>
        /// <param name="filePath">檔案完整路徑</param>
        /// <returns>檔案是否存在</returns>
        /// <exception cref="ArgumentNullException">filePath為null或空字串時拋出</exception>
        public static bool CheckFileExist(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "檔案路徑不可為空");

            return File.Exists(filePath);
        }

        /// <summary>
        /// 取得指定資料夾內所有檔案的完整路徑清單
        /// </summary>
        /// <param name="folderPath">資料夾完整路徑</param>
        /// <param name="isSearchSubFolder">是否往下撈子資料夾檔案</param>
        /// <param name="subFolderDepth">子資料夾底層層數</param>
        /// <param name="currentFolderDepth">目前層數</param>
        /// <returns>該資料夾底下的所有檔案</returns>
        private static List<string> GetAllFileInFolder(string folderPath, int currentFolderDepth, bool isSearchSubFolder = false, int subFolderDepth = -1)
        {
            List<string> filePathList = new List<string>();

            if (!CheckFolderExist(folderPath))
                return filePathList;

            filePathList.AddRange(Directory.GetFiles(folderPath).ToList());

            if (isSearchSubFolder && (subFolderDepth == -1 || currentFolderDepth < subFolderDepth))
                foreach (string subFolder in Directory.GetDirectories(folderPath))
                    filePathList.AddRange(GetAllFileInFolder(subFolder, currentFolderDepth + 1, true, subFolderDepth));

            return filePathList;
        }
    }
}
