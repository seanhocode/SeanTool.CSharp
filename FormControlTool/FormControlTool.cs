namespace SeanTool.CSharp.Net8.Forms
{
    /* DockStyle:
            None       // 不停靠，使用 Location + Size
            Top        // 停靠到父容器上方
            Bottom     // 停靠到父容器下方
            Left       // 停靠到父容器左側
            Right      // 停靠到父容器右側
            Fill       // 填滿整個父容器
    */
    public static class FormControlTool
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

        /// <summary>
        /// 生成一個DataGridView的ButtonColumn
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataGridView"></param>
        /// <param name="dataGridViewButtonColumnName"></param>
        /// <param name="actionColName"></param>
        /// <param name="btnText"></param>
        /// <param name="callback"></param>
        public static void GenDataGridViewActionColumn<T>(DataGridView dataGridView, string dataGridViewButtonColumnName, string actionColName, string btnText, int index, Action<T> callback)
        {
            DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn();
            btnCol.Name = dataGridViewButtonColumnName;
            btnCol.HeaderText = actionColName;
            btnCol.Text = btnText;
            //true：
            //    每個按鈕格子都會顯示buttonColumn.Text的值
            //    簡單說：所有列的按鈕都會顯示一樣的文字
            //false：
            //    系統會從該儲存格的值(cell.Value)來顯示文字
            //    可以針對不同的列，設定不同的按鈕文字
            btnCol.UseColumnTextForButtonValue = true;
            btnCol.DisplayIndex = index;

            dataGridView.Columns.Add(btnCol);

            dataGridView.CellClick += (sender, e) =>
            {
                var dgv = sender as DataGridView;

                // 確保不是點到標題列或空白列
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                // 檢查是否是你按鈕的那一欄
                if (dgv.Columns[e.ColumnIndex].Name == dataGridViewButtonColumnName)
                    //抓出這列綁定的資料物件並執行 callback
                    if (dgv.Rows[e.RowIndex].DataBoundItem is T item)
                        callback?.Invoke(item);
            };
        }
    }
}
