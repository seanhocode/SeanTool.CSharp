using System.Diagnostics;
using System.Text;

namespace SeanTool.Tools
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

        public static void DeleteFolder(string targetFolder)
        {
            string[] files = Directory.GetFiles(targetFolder, "*", SearchOption.AllDirectories);
            Parallel.ForEach(files, (file) => File.Delete(file));

            string[] folders = Directory.GetDirectories(targetFolder, "*", SearchOption.AllDirectories);
            Array.Reverse(folders);
            Parallel.ForEach(folders, (folder) => Directory.Delete(folder, false));

            Directory.Delete(targetFolder, false);
        }

        public static string DeleteFolderByCommand(string targetFolder)
        {
            string cmdCommand = @$"cmd /c rd /s /q ""{targetFolder}""";
            string output = string.Empty;
            Process process = _NewProcess("cmd.exe", @"C:\", cmdCommand, true);

            process.OutputDataReceived += (sender, args) => { output += $"[Msg]{args.Data ?? string.Empty}"; };
            process.ErrorDataReceived += (sender, args) => { output += $"[Err]{args.Data ?? string.Empty}"; };

            process.Exited += (s, e) =>
            {
                process.Dispose();
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                output = ex.ToString();
            }

            return output;
        }

        /// <summary>
        /// 建立啟動外部應用程式的Process
        /// </summary>
        /// <param name="fileName">外部應用程式檔名</param>
        /// <param name="fileFolderPath">外部應用程式檔案所在資料夾</param>
        /// <param name="arguments">傳給執行程式的命令列參數(Command Line Arguments)</param>
        /// <param name="isFullPath">fileName是否為絕對路徑，不是的話會組合fileName、fileFolderPath(預設:否)</param>
        /// <remarks>不會檢查檔案是否存在，呼叫端需自行確認</remarks>
        /// <returns>Process物件(尚未啟動)</returns>
        private static Process _NewProcess(string fileName, string fileFolderPath, string arguments, bool isFullPath = false)
        {
            fileName = isFullPath ? fileName : Path.Combine(fileFolderPath, fileName);
            arguments = arguments == null ? string.Empty : arguments;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,                    // 指定執行檔案檔名(完整路徑)
                WorkingDirectory = fileFolderPath,      // 設定執行命令時的工作目錄，如果fileName是相對路徑，系統會在這個目錄下找
                UseShellExecute = false,                // 決定是否使用作業系統的Shell來啟動程序，false才可以進一步設定RedirectStandardOutput和RedirectStandardError
                RedirectStandardOutput = true,          // 將標準輸出(Standard Output)重定向，允許你從程式中取得批次檔的輸出內容
                RedirectStandardError = true,           // 同上，標準錯誤(Standard Error)，如果批次檔執行中有錯誤訊息，你可以從程式中讀取
                CreateNoWindow = true,                  // 表示不要顯示任何命令視窗，執行時會完全在背景中運作
                WindowStyle = ProcessWindowStyle.Hidden,// 適用於GUI應用程式，可隱藏或控制視窗顯示樣式(不適用Console程式)
                StandardOutputEncoding = Encoding.UTF8, //
                StandardErrorEncoding = Encoding.UTF8,  //
            };

            if (!string.IsNullOrWhiteSpace(arguments)) psi.Arguments = arguments;

            //這個屬性若設為 true，表示當這個 Process 結束執行時，會觸發 Exited 事件
            return new Process { StartInfo = psi, EnableRaisingEvents = true };
        }
    }
}
