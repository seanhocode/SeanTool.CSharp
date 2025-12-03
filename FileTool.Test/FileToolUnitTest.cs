using System.Diagnostics;

namespace SeanTool.CSharp.Net8.Test
{
    public class FileToolUnitTest
    {
        // 每個測試方法都用 [Fact] 標記 (xUnit.net)

        [Fact]
        public void VarableCheck()
        {
            Assert.Equal(FileTool.ThisExePath, Process.GetCurrentProcess().MainModule!.FileName);
            Assert.Equal(FileTool.ThisExeDir, Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName));
        }

        [Fact]
        public void CheckFolderExistTest()
        {
            string testFolderPath = Path.Combine(FileTool.ThisExeDir, "RootFolderNoExist");
            // 確保資料夾不存在
            if (Directory.Exists(testFolderPath))
                Directory.Delete(testFolderPath, true);

            // 測試資料夾不存在且不自動建立
            Assert.False(FileTool.CheckFolderExist(testFolderPath, false));

            // 測試資料夾不存在且自動建立
            Assert.True(FileTool.CheckFolderExist(testFolderPath, true));
            Assert.True(Directory.Exists(testFolderPath));

            // 清理測試資料夾
            Directory.Delete(testFolderPath, true);
        }

        [Fact]
        public void CheckFileExistTest()
        {
            // Do測試
            string testFilePath = Path.Combine(FileTool.ThisExeDir, "TestFileNoExist.txt");

            // 確保檔案不存在
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);

            // 測試檔案不存在
            Assert.False(FileTool.CheckFileExist(testFilePath));

            // 建立測試檔案
            File.WriteAllText(testFilePath, "This is a test file.");

            // 測試檔案存在
            Assert.True(FileTool.CheckFileExist(testFilePath));

            // 清理測試檔案
            File.Delete(testFilePath);
        }

        [Fact]
        public void GetAllFileInFolderTest()
        {
            List<(int FolderDepth, string FilePath)> folderPathList = new List<(int FolderDepth, string FilePath)>
            {
                (4, "RootFolder\\Depth1Folder1\\Depth2Folder1\\Depth3Folder1\\Depth4Folder1\\TestFile1.txt" ),
                (4, "RootFolder\\Depth1Folder1\\Depth2Folder1\\Depth3Folder1\\Depth4Folder1\\TestFile2.txt" ),
                (4, "RootFolder\\Depth1Folder1\\Depth2Folder1\\Depth3Folder1\\Depth4Folder1\\TestFile3.txt" ),
                (3, "RootFolder\\Depth1Folder1\\Depth2Folder1\\Depth3Folder1\\TestFile4.txt" ),
                (2, "RootFolder\\Depth1Folder1\\Depth2Folder1\\TestFile5.txt" ),
                (1, "RootFolder\\Depth1Folder1\\TestFile6.txt" ),
                (0, "RootFolder\\TestFile7.txt" ),
                (3, "RootFolder\\Depth1Folder1\\Depth2Folder2\\Depth3Folder1\\TestFile8.txt" ),
                (2, "RootFolder\\Depth1Folder2\\Depth2Folder1\\TestFile9.txt" ),
                (2, "RootFolder\\Depth1Folder2\\Depth2Folder2\\TestFile10.txt" ),
                (1, "RootFolder\\Depth1Folder3\\TestFile11.txt" )
            };

            // 建立測試資料夾及檔案
            string testFolderPath = Path.Combine(FileTool.ThisExeDir, "RootFolder");
            foreach ((int FolderDepth, string FilePath) relativePath in folderPathList)
            {
                string fullPath = Path.Combine(FileTool.ThisExeDir, relativePath.FilePath);
                string dirPath = Path.GetDirectoryName(fullPath)!;
                Directory.CreateDirectory(dirPath);
                File.WriteAllText(fullPath, fullPath);
            }

            // 測試不搜尋子資料夾
            List<string> files = FileTool.GetAllFileInFolder(testFolderPath, false);
            Assert.Single(files);
            Assert.Contains(Path.Combine(testFolderPath, "TestFile7.txt"), files);

            // 測試搜尋所有子資料夾
            files = FileTool.GetAllFileInFolder(testFolderPath, true);
            Assert.Equal(folderPathList.Count, files.Count);
            foreach ((int FolderDepth, string FilePath) relativePath in folderPathList)
            {
                string expectedFilePath = Path.Combine(FileTool.ThisExeDir, relativePath.FilePath);
                Assert.Contains(expectedFilePath, files);
            }

            // 測試搜尋子資料夾到指定深度(2層)
            List<(int FolderDepth, string FilePath)> folderPathListDepth2 
                = folderPathList.Where(f => f.FolderDepth <= 2).ToList();

            files = FileTool.GetAllFileInFolder(testFolderPath, true, 2);
            Assert.Equal(folderPathListDepth2.Count, files.Count);
            
            foreach ((int FolderDepth, string FilePath) relativePath in folderPathListDepth2)
            {
                string expectedFilePath = Path.Combine(FileTool.ThisExeDir, relativePath.FilePath);
                Assert.Contains(expectedFilePath, files);
            }
        }

        [Fact]
        public void DeleteFolderTest()
        {
            string deleteFolderPath = Path.Combine(FileTool.ThisExeDir, "DeleteData");
            Directory.CreateDirectory(deleteFolderPath);
            for (int i = 0; i < 10; i++)
                File.WriteAllText(Path.Combine(deleteFolderPath, $"{i.ToString()}.txt"), i.ToString());

            FileTool.DeleteFolder(deleteFolderPath);

            Assert.False(Directory.Exists(deleteFolderPath));
        }

        [Fact]
        public void ReadFileTest()
        {
            string filePath = Path.Combine(FileTool.ThisExeDir, "Data", "TestLog.log");

            int dummy = 0, lineCount = 0;
            Action<string> processorSync = line =>
            {
                dummy += line.Length;
                lineCount++;
            };

            Stopwatch sw = Stopwatch.StartNew();
            foreach (string line in FileTool.ReadFile(filePath, 80 * 1024))
            {
                processorSync(line);
            }
            sw.Stop();

            Assert.Equal(10000004, lineCount);
            Assert.True(sw.ElapsedMilliseconds < 2000);
        }

        [Fact]
        public async Task ReadFileAsyncTest()
        {
            string filePath = Path.Combine(FileTool.ThisExeDir, "Data", "TestLog.log");

            int dummy = 0, lineCount = 0;
            Func<string, Task> processorASync = line =>
            {
                dummy += line.Length;
                lineCount++;
                return Task.CompletedTask;
            };

            Stopwatch sw = Stopwatch.StartNew();
            await foreach(string line in FileTool.ReadFileAsync(filePath, 80 * 1024)){
                await processorASync(line);
            }
            sw.Stop();

            Assert.Equal(10000004, lineCount);
            Assert.True(sw.ElapsedMilliseconds < 10000);
        }
    }
}