using System.IO.Compression;

namespace ZipTool
{
    public static class ZipTool
    {
        /// <summary>
        /// 取得指定ZIP壓縮檔中所有檔案(與資料夾)清單
        /// </summary>
        /// <param name="zipPath">ZIP壓縮檔的完整路徑</param>
        /// <remarks>不進行解壓縮，只列出內部的檔案名稱(含相對路徑)</remarks>
        /// <returns>ZIP中所有檔案(與資料夾)的路徑清單，如"folder/file.txt"</returns>
        public static IList<string> GetFileNameInZip(string zipPath)
        {
            IList<string> fileNameList = new List<string>();

            if (!CheckZipFile(zipPath))
                return fileNameList;

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                    fileNameList.Add(entry.FullName.Replace('/', '\\'));
            }

            return fileNameList.OrderBy(name => name).ToList();
        }

        /// <summary>
        /// 檢查指定的檔案是否為合法的ZIP壓縮檔格式
        /// </summary>
        /// <param name="zipPath">ZIP檔案的完整路徑</param>
        /// <remarks>不會解壓縮檔案，只會嘗試開啟並確認ZIP結構是否正常</remarks>
        /// <returns>如果是合法的ZIP檔案則回傳true，否則回傳false</returns>
        private static bool CheckZipFile(string zipPath)
        {
            if (!File.Exists(zipPath) || !zipPath.ToLower().EndsWith(".zip"))
                return false;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    return true;
                }
            }
            catch (InvalidDataException ex)
            {
                throw new Exception("該檔案不是合法的 ZIP 格式！");
                throw new Exception($"錯誤訊息：{ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception("發生其他錯誤：");
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 從指定的 ZIP 檔案中，提取單一檔案的內容並讀取到記憶體(解壓縮單一檔案)
        /// </summary>
        /// <param name="zipPath">ZIP 檔案的完整路徑</param>
        /// <param name="targetEntryName">要提取的目標檔案在 ZIP 檔案中的名稱或相對路徑，例如："Data/File.txt"</param>
        /// <returns>如果 ZIP 檔案無效或不存在，則回傳 null；否則回傳目標檔案內容的 byte 陣列</returns>
        public static byte[] ExtractSingleFileToMemory(string zipPath, string targetEntryName)
        {
            if (!CheckZipFile(zipPath)) return Array.Empty<byte>(); // 推薦使用 Array.Empty<byte>()

            // Step.1: 將 Windows 反斜線轉換為 ZIP 標準的正斜線
            // 並確保路徑沒有開頭的斜線 (這在 GetEntry 中通常會失敗)
            string standardizedName = targetEntryName
                .Replace('\\', '/')
                .TrimStart('/');

            // Step.2: 嘗試使用標準化的名稱獲取條目
            using (var zipStream = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // GetEntry 要求嚴格匹配。如果失敗，則進行Step.3
                var entry = archive.GetEntry(standardizedName);

                if (entry == null)
                {
                    // Step.3 如果嚴格匹配失敗，遍歷所有條目，嘗試尋找名稱相似的條目
                    entry = archive.Entries
                       .FirstOrDefault(e => e.FullName.EndsWith(standardizedName) || e.FullName.EndsWith(standardizedName.Replace('/', '\\')));
                }

                if (entry == null)
                {
                    throw new Exception($"ZIP 中找不到檔案：{targetEntryName}");
                }

                using (var entryStream = entry.Open())
                using (var ms = new MemoryStream())
                {
                    entryStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
