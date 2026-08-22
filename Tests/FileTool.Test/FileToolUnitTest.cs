using System.Diagnostics;
using System.Text;

namespace SeanTool.CSharp.FileTool.Test
{
    public class FileToolUnitTest
    {
        // �C�Ӵ��դ�k���� [Fact] �аO (xUnit.net)

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
            // �T�O��Ƨ����s�b
            if (Directory.Exists(testFolderPath))
                Directory.Delete(testFolderPath, true);

            // ���ո�Ƨ����s�b�B���۰ʫإ�
            Assert.False(FileTool.CheckFolderExist(testFolderPath, false));

            // ���ո�Ƨ����s�b�B�۰ʫإ�
            Assert.True(FileTool.CheckFolderExist(testFolderPath, true));
            Assert.True(Directory.Exists(testFolderPath));

            // �M�z���ո�Ƨ�
            Directory.Delete(testFolderPath, true);
        }

        [Fact]
        public void CheckFileExistTest()
        {
            // Do����
            string testFilePath = Path.Combine(FileTool.ThisExeDir, "TestFileNoExist.txt");

            // �T�O�ɮפ��s�b
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);

            // �����ɮפ��s�b
            Assert.False(FileTool.CheckFileExist(testFilePath));

            // �إߴ����ɮ�
            File.WriteAllText(testFilePath, "This is a test file.");

            // �����ɮצs�b
            Assert.True(FileTool.CheckFileExist(testFilePath));

            // �M�z�����ɮ�
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

            // �إߴ��ո�Ƨ����ɮ�
            string testFolderPath = Path.Combine(FileTool.ThisExeDir, "RootFolder");
            foreach ((int FolderDepth, string FilePath) relativePath in folderPathList)
            {
                string fullPath = Path.Combine(FileTool.ThisExeDir, relativePath.FilePath);
                string dirPath = Path.GetDirectoryName(fullPath)!;
                Directory.CreateDirectory(dirPath);
                File.WriteAllText(fullPath, fullPath);
            }

            // ���դ��j�M�l��Ƨ�
            List<string> files = FileTool.GetAllFileInFolder(testFolderPath, false);
            Assert.Single(files);
            Assert.Contains(Path.Combine(testFolderPath, "TestFile7.txt"), files);

            // ���շj�M�Ҧ��l��Ƨ�
            files = FileTool.GetAllFileInFolder(testFolderPath, true);
            Assert.Equal(folderPathList.Count, files.Count);
            foreach ((int FolderDepth, string FilePath) relativePath in folderPathList)
            {
                string expectedFilePath = Path.Combine(FileTool.ThisExeDir, relativePath.FilePath);
                Assert.Contains(expectedFilePath, files);
            }

            // ���շj�M�l��Ƨ�����w�`��(2�h)
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
            DateTime baseTime = DateTime.Now.Date;
            string testLogName = $"u_ex{baseTime.ToString("yyMMdd")}.log";

            string filePath = Path.Combine(FileTool.ThisExeDir, "Data", testLogName);

            GenTestLog(filePath);

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

            Assert.Equal(10_000_004, lineCount);
            Assert.True(sw.ElapsedMilliseconds < 5_000);

            string dirPath = Path.GetDirectoryName(filePath)!;
            if (Directory.Exists(dirPath) && !Directory.EnumerateFileSystemEntries(dirPath).Any())
                Directory.Delete(dirPath);
        }

        [Fact]
        public async Task ReadFileAsyncTest()
        {
            
            DateTime baseTime = DateTime.Now.Date;
            string testLogName = $"u_ex{baseTime.ToString("yyMMdd")}.log";

            string filePath = Path.Combine(FileTool.ThisExeDir, "Data", testLogName);

            GenTestLog(filePath);

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

            Assert.Equal(10_000_004, lineCount);
            Assert.True(sw.ElapsedMilliseconds < 10_000);

            File.Delete(filePath);

            string dirPath = Path.GetDirectoryName(filePath)!;
            if (Directory.Exists(dirPath) && !Directory.EnumerateFileSystemEntries(dirPath).Any())
                Directory.Delete(dirPath);
        }

        private void GenTestLog(string logPath){
            DateTime baseTime = DateTime.Now.Date;
            const int lines = 10_000_000;
            var sb = new StringBuilder(200);

            if(!Directory.Exists(Path.GetDirectoryName(logPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            var rand = new Random();

            using (var fs = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024))
            using (var sw = new StreamWriter(fs))
            {
                // ---- IIS Header ----
                sw.WriteLine("#Software: Microsoft Internet Information Services 10.0");
                sw.WriteLine("#Version: 1.0");
                sw.WriteLine($"#Date: {baseTime.ToString("yyyy-MM-dd HH:mm:ss")}");
                sw.WriteLine("#Fields: date time cs-method cs-uri-stem sc-status sc-bytes cs-bytes time-taken");

                var currentTime = baseTime;

                for (int i = 0; i < lines; i++)
                {
                    // ---- �Ҧ� B�G���W�ɶ� + �H�� 0~50ms�]����t���^ ----
                    currentTime = currentTime.AddMilliseconds(rand.Next(0, 50));

                    sb.Clear();
                    sb.Append(currentTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.Append(" GET /page?id=");
                    sb.Append(i);
                    sb.Append(" 200 1024 512 ");
                    sb.Append(rand.Next(1, 200)); // time-taken

                    sw.WriteLine(sb);
                }
            }
        }
    }
}