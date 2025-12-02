using System.Text;

namespace SeanTool.Tools.Test
{
    public class ZipToolUnitTest
    {
        [Fact]
        public void GetFileNameInZipTest()
        {
            IList<string> filePathList = ZipTool.GetFileNameInZip(".\\Data\\Zip1.zip");
            IList<string> filePathCurrectList = new List<string>()
            {
                "Zip1\\Depth1Folder1\\Depth2Folder1\\Depth3File1.txt",
                "Zip1\\Depth1Folder1\\Depth2Folder1\\Depth3File2.txt",
                "Zip1\\Depth1Folder1\\Depth2Folder1\\Depth3File3.txt",
                "Zip1\\Depth1Folder1\\Depth2Folder2\\Depth3File1.txt",
                "Zip1\\Depth1Folder1\\Depth2File1.txt",
                "Zip1\\Depth1Folder2\\Depth2File1.txt",
                "Zip1\\Depth1File1.txt",
                "Zip1\\Depth1File2.txt",
                "Zip1\\",
                "Zip1\\Depth1Folder1\\",
                "Zip1\\Depth1Folder1\\Depth2Folder1\\",
                "Zip1\\Depth1Folder1\\Depth2Folder2\\",
                "Zip1\\Depth1Folder2\\"
            };
            filePathCurrectList = filePathCurrectList.OrderBy(name => name).ToList();

            Assert.Equal(filePathCurrectList.Count, filePathList.Count);

            for(int i = 0; i < filePathCurrectList.Count; i++)
                Assert.Equal(filePathCurrectList[i], filePathList[i]);
        }

        [Fact]
        public void CheckZipFileTest(){
            string zipPathValid = ".\\Data\\Zip1.zip",
                zipPathInvalid = ".\\Data\\NotZip.txt",
                zipPathNoExist = ".\\Data\\NoExist.zip";

            // 測試合法的 ZIP 檔案
            bool isValidZip = ZipTool.GetFileNameInZip(zipPathValid).Count > 0;
            Assert.True(isValidZip);

            // 測試不合法的 ZIP 檔案
            IList<string> filePathList = ZipTool.GetFileNameInZip(zipPathInvalid);
            Assert.Empty(filePathList);

            // 測試不存在的 ZIP 檔案
            IList<string> fileListNoExist = ZipTool.GetFileNameInZip(zipPathNoExist);
            Assert.Empty(fileListNoExist);
        }

        [Fact]
        public void ExtractSingleFileToMemoryTest()
        {
            string zipPath = ".\\Data\\Zip1.zip",
                resultString = string.Empty;
            byte[] resultBytes;

            IList<string> filePathList = ZipTool.GetFileNameInZip(zipPath);

            foreach(string filePath in filePathList)
            {
                if(filePath.EndsWith("\\"))
                    continue;
                resultBytes = ZipTool.ExtractSingleFileToMemory(zipPath, filePath);
                resultString = Encoding.UTF8.GetString(resultBytes);

                Assert.Equal(resultString, filePath);
            }
        }
    }
}