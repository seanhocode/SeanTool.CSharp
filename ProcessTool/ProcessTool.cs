using System.Diagnostics;
using System.Text;

namespace SeanTool.Tools
{
    public static class ProcessTool
    {
        /// <summary>
        /// 建立啟動外部應用程式的Process
        /// </summary>
        /// <param name="fileName">外部應用程式檔名</param>
        /// <param name="fileFolderPath">外部應用程式檔案所在資料夾</param>
        /// <param name="arguments">傳給執行程式的命令列參數(Command Line Arguments)</param>
        /// <param name="isFullPath">fileName是否為絕對路徑，不是的話會組合fileName、fileFolderPath(預設:否)</param>
        /// <remarks>不會檢查檔案是否存在，呼叫端需自行確認</remarks>
        /// <returns>Process物件(尚未啟動)</returns>
        public static Process NewProcess(string fileName, string fileFolderPath, string arguments, bool isFullPath = false)
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

        /// <summary>
        /// 建立啟動外部應用程式的Process
        /// </summary>
        /// <param name="fullFilePath">外部應用程式檔絕對路徑</param>
        /// <param name="arguments">傳給執行程式的命令列參數(Command Line Arguments)</param>
        /// <remarks>不會檢查檔案是否存在，呼叫端需自行確認</remarks>
        /// <returns>Process物件(尚未啟動)</returns>
        public static Process NewProcess(string fullFilePath, string arguments)
        {
            return NewProcess(fullFilePath, Path.GetDirectoryName(fullFilePath), arguments, true);
        }
    }
}
