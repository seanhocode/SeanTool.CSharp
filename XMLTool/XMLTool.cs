using System.Xml.Linq;

namespace XMLTool
{
    public static class XMLTool
    {
        /// <summary>
        /// 從byte陣列讀取並載入XML文件
        /// </summary>
        /// <param name="byteData">包含XML內容的byte陣列</param>
        /// <returns>XDocument物件</returns>
        /// <exception cref="ArgumentException">當byteData為null或空陣列時拋出</exception>
        public static XDocument GetXDocumentFromBytes(byte[]? byteData)
        {
            if (byteData == null || byteData.Length == 0)
                throw new ArgumentException("資料不可為空", nameof(byteData));

            using (var ms = new MemoryStream(byteData))
            {
                return XDocument.Load(ms);
            }
        }

        /// <summary>
        /// 取得XDocument
        /// </summary>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static XDocument GetXDocument(string xmlPath)
        {
            if (!File.Exists(xmlPath))
                throw new Exception($"找不到{xmlPath}");


            File.SetAttributes(xmlPath, FileAttributes.Normal); // 解除唯讀

            return XDocument.Load(xmlPath);
        }

        /// <summary>
        /// 存檔
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="xmlPath"></param>
        public static void SaveXML(XDocument xmlDoc, string xmlPath)
        {
            xmlDoc.Save(xmlPath);
        }
    }
}
