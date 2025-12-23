using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace SeanTool.CSharp
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

        /// <summary>
        /// 刪除資料夾及其所有內容
        /// </summary>
        /// <param name="targetFolder"></param>
        public static void DeleteFolder(string targetFolder)
        {
            string[] files = Directory.GetFiles(targetFolder, "*", SearchOption.AllDirectories);
            Parallel.ForEach(files, (file) => File.Delete(file));

            string[] folders = Directory.GetDirectories(targetFolder, "*", SearchOption.AllDirectories);
            Array.Reverse(folders);
            Parallel.ForEach(folders, (folder) => Directory.Delete(folder, false));

            Directory.Delete(targetFolder, false);
        }

        /// <summary>
        /// 讀取檔案內容
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="bufferSize">Buffer 大小，預設 80KB</param>
        /// <returns>一個可逐行列舉的字串序列</returns>
        public static IEnumerable<string> ReadFile(
            string filePath,
            int bufferSize = 80 * 1024
        )
        {
            FileStreamOptions fileOptions = new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.SequentialScan,
                BufferSize = bufferSize
            };

            using StreamReader reader = new StreamReader(filePath, fileOptions);

            string? line;
            while ((line = reader.ReadLine()) != null){
                yield return line;
            }
        }

        // IEnumerable 是一個可逐項走訪的序列的抽象協定
        // yield return 讓方法變成可逐項產生資料的迭代器（iterator）的語法糖
        /// <summary>
        /// 非同步讀取檔案內容
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="bufferSize">Buffer 大小，預設 80KB</param>
        /// <returns>一個可逐行列舉的字串序列</returns>
        public static async IAsyncEnumerable<string> ReadFileAsync(
            string filePath,
            int bufferSize = 80 * 1024
        )
        {

            string? line;
            FileStreamOptions fileOptions = new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
                BufferSize = bufferSize
            };
            using StreamReader reader = new StreamReader(filePath, fileOptions);

            while ((line = await reader.ReadLineAsync()) != null)
                yield return line;
        }

        # region 測試後效能較差
        /// <summary>
        /// 讀取文字檔
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="lineProcessor"></param>
        /// <param name="bufferSize">初始 Buffer 大小 (預設 80KB)</param>
        /// <param name="encoding">文字編碼 (預設 UTF-8)</param>
        /// <param name="cancellationToken">取消權杖</param>
        private static async Task ReadFileAsyncByBuffer(
            string filePath,
            Func<string, Task> lineProcessor,
            int bufferSize = 80 * 1024,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default
        ){
            
            if (!CheckFileExist(filePath)) throw new FileNotFoundException("File not found", filePath);

            encoding ??= Encoding.UTF8; // 預設 UTF-8
            byte[] buffer = new byte[bufferSize];
            int activeBytes = 0;

            // Step.1 設定 FileStream
            FileStreamOptions fileOptions = new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.  ReadWrite,
                Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
                BufferSize = bufferSize
            };

            using FileStream fileStream = new FileStream(filePath, fileOptions);

            try
            {
                while (true)
                {
                    // Step.2 從檔案讀取資料，填入 Buffer
                    // offset : 接在目前殘留資料(activeBytes)的後面
                    // count  : 填滿 Buffer(Buffer空間 - 殘留資料 = 剩餘空間)
                    // 支援 CancellationToken (如果外部取消，這裡會拋出 OperationCanceledException)
                    int bytesRead = await fileStream.ReadAsync(buffer, activeBytes, buffer.Length - activeBytes, cancellationToken);
                    // 目前 Buffer 內的總有效位元組數(殘留 +新讀入)
                    int totalBytes = activeBytes + bytesRead;
                    // 已處理完的游標位置(每行開始位置，是最後一個 \n 的下一個位置)
                    int currentLineStart = 0;

                    // 讀不到資料且無殘留 -> 結束
                    if (totalBytes == 0) break;

                    // Step.3 解析換行
                    for (int i = 0; i < totalBytes; i++)
                    {
                        // UTF-8，byte 10 => '\n'
                        // UTF-8，byte 10 => '\n'
                        if (buffer[i] == 10) 
                        {
                            int lineLength = i - currentLineStart;
                            // 去除 '\r'
                            if (lineLength > 0 && buffer[i - 1] == 13) lineLength--; 

                            await lineProcessor(encoding.GetString(buffer, currentLineStart, lineLength));

                            currentLineStart = i + 1;
                        }
                    }

                    // Step.4 剩下沒處理到的資料長度 = 資料總長 - 目前行開始位置
                    int leftBytes = totalBytes - currentLineStart;

                    if (leftBytes > 0)
                    {
                        if (bytesRead == 0) // EOF
                        {
                            await lineProcessor(encoding.GetString(buffer, currentLineStart, leftBytes));

                            break;
                        }
                        else
                        {
                            // 如果殘留資料塞滿了整個 Buffer，代表單行超過 Buffer 上限
                            if (leftBytes == buffer.Length)
                            {
                                int newSize = buffer.Length * 2;
                                // 設定上限防止記憶體耗盡 (例如限制單行最大 100MB)
                                if (newSize > 100 * 1024 * 1024)
                                    throw new InvalidOperationException($"Line too long (exceeded 100MB limit).");

                                byte[] newBuffer = new byte[newSize];
                                // 將舊資料搬到新 Buffer
                                Array.Copy(buffer, currentLineStart, newBuffer, 0, leftBytes);
                                // 替換參考
                                buffer = newBuffer;
                            }
                            else
                            {
                                Array.Copy(buffer, currentLineStart, buffer, 0, leftBytes);
                            }

                            activeBytes = leftBytes;
                        }
                    }
                    else
                    {
                        activeBytes = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 讀取每一行並執行傳入的處理邏輯
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="lineProcessor">自訂處理邏輯 (輸入字串，回傳 Task)</param>
        /// <param name="cancellationToken">取消權杖</param>
        private static async Task ProcessLinesAsync(
          string filePath,
          Func<string, Task> lineProcessor,
          CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(1000)
            { SingleWriter = true, SingleReader = true });

            Task readTask = ReadFileToChannelAsync(
              filePath,
              channel.Writer,
              bufferSize: 1024 * 80,
              encoding: Encoding.UTF8,
              cancellationToken: cancellationToken
            );

            try
            {
                await foreach (string line in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    //呼叫傳入的 function
                    await lineProcessor(line);
                }

                await readTask; // 確保讀取正常結束
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 讀取文字檔並寫入 Channel
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="writer">Channel.Writer</param>
        /// <param name="bufferSize">初始 Buffer 大小 (預設 80KB)</param>
        /// <param name="encoding">文字編碼 (預設 UTF-8)</param>
        /// <param name="cancellationToken">取消權杖</param>
        private static async Task ReadFileToChannelAsync(
          string filePath,
          ChannelWriter<string> writer,
          int bufferSize = 80 * 1024,
          Encoding? encoding = null,
          CancellationToken cancellationToken = default
        )
        {

            if (!CheckFileExist(filePath)) throw new FileNotFoundException("File not found", filePath);
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            encoding ??= Encoding.UTF8; // 預設 UTF-8
            byte[] buffer = new byte[bufferSize];
            int activeBytes = 0;

            // Step.1 設定 FileStream
            FileStreamOptions fileOptions = new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
                BufferSize = bufferSize
            };

            using FileStream fileStream = new FileStream(filePath, fileOptions);

            try
            {
                while (true)
                {
                    // Step.2 從檔案讀取資料，填入 Buffer
                    // offset : 接在目前殘留資料(activeBytes)的後面
                    // count  : 填滿 Buffer(Buffer空間 - 殘留資料 = 剩餘空間)
                    // 支援 CancellationToken (如果外部取消，這裡會拋出 OperationCanceledException)
                    int bytesRead = await fileStream.ReadAsync(buffer, activeBytes, buffer.Length - activeBytes, cancellationToken);
                    // 目前 Buffer 內的總有效位元組數(殘留 +新讀入)
                    int totalBytes = activeBytes + bytesRead;
                    // 已處理完的游標位置(每行開始位置，是最後一個 \n 的下一個位置)
                    int currentLineStart = 0;

                    // 讀不到資料且無殘留 -> 結束
                    if (totalBytes == 0) break;

                    // Step.3 解析換行
                    for (int i = 0; i < totalBytes; i++)
                    {
                        // UTF-8，byte 10 => '\n'
                        // UTF-8，byte 10 => '\n'
                        if (buffer[i] == 10)
                        {
                            int lineLength = i - currentLineStart;
                            // 去除 '\r'
                            if (lineLength > 0 && buffer[i - 1] == 13) lineLength--;

                            string line = encoding.GetString(buffer, currentLineStart, lineLength);

                            // 寫入 Channel (也要支援取消)
                            await writer.WriteAsync(line, cancellationToken);

                            currentLineStart = i + 1;
                        }
                    }

                    // Step.4 剩下沒處理到的資料長度 = 資料總長 - 目前行開始位置
                    int leftBytes = totalBytes - currentLineStart;

                    if (leftBytes > 0)
                    {
                        if (bytesRead == 0) // EOF
                        {
                            string lastLine = encoding.GetString(buffer, currentLineStart, leftBytes);
                            await writer.WriteAsync(lastLine, cancellationToken);
                            activeBytes = 0;
                        }
                        else
                        {
                            // 如果殘留資料塞滿了整個 Buffer，代表單行超過 Buffer 上限
                            if (leftBytes == buffer.Length)
                            {
                                int newSize = buffer.Length * 2;
                                // 設定上限防止記憶體耗盡 (例如限制單行最大 100MB)
                                if (newSize > 100 * 1024 * 1024)
                                    throw new InvalidOperationException($"Line too long (exceeded 100MB limit).");

                                byte[] newBuffer = new byte[newSize];
                                // 將舊資料搬到新 Buffer
                                Array.Copy(buffer, currentLineStart, newBuffer, 0, leftBytes);
                                // 替換參考
                                buffer = newBuffer;
                                activeBytes = leftBytes;
                            }
                            else
                            {
                                Array.Copy(buffer, currentLineStart, buffer, 0, leftBytes);
                                activeBytes = leftBytes;
                            }
                        }
                    }
                    else
                    {
                        activeBytes = 0;
                    }
                }

                writer.Complete();
            }
            catch (Exception ex)
            {
                // 發生錯誤 (包含被取消) 時，將例外傳給 Consumer
                writer.Complete(ex);
            }
        }
        # endregion
    }
}
