using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace SeanTool.CSharp.Forms
{
    /* ToDo
     * 1. SortCompare
     * 2. Filter
     * 3. Paging
     */

     /// <summary>
     /// 欄位定義
     /// </summary>
     /// <typeparam name="T"></typeparam>
    public class GridColumnDefinition<T>
    {
        /// <summary>
        /// 是否唯讀
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// 欄位值
        /// </summary>
        /// <remarks>ex.按鈕名稱</remarks>
        public string ColumnValue { get; set; } = string.Empty;

        /// <summary>
        /// 表頭顯示名稱
        /// </summary>
        public string HeaderText { get; set; } = string.Empty;

        /// <summary>
        /// 欄位寬度
        /// </summary>
        public int Width { get; set; } = 50;

        /// <summary>
        /// 對齊模式
        /// </summary>
        public DataGridViewContentAlignment Alignment { get; set; } = DataGridViewContentAlignment.MiddleCenter;

        /// <summary>
        /// 是否為按鈕欄位
        /// </summary>
        public bool IsButtonColumn { get; set; } = false;

        /// <summary>
        /// 按鈕動作
        /// </summary>
        public Action<T>? ButtonClickAction { get; set; } = null;
    }

    /// <summary>
    /// Grid模式
    /// </summary>
    public enum GridEditMode
    {
        /// <summary>
        /// 檢視
        /// </summary>
        ReadOnly,
        /// <summary>
        /// 編輯
        /// </summary>
        Editable
    }

    public static class GridColumnExtensions
    {
        /// <summary>
        /// 產生預設的 GridColumnDefinition 清單
        /// </summary>
        public static IList<GridColumnDefinition<T>> GenerateDefaultColumns<T>(this IEnumerable<T> source)
        {
            return typeof(T).GenerateDefaultColumns<T>();
        }

        /// <summary>
        /// 產生預設的 GridColumnDefinition 清單
        /// </summary>
        /// <remarks>
        /// <para>只抓public</para>
        /// <para>屬性有設定DisplayName，會帶入HeaderText</para>
        /// <para>屬性有設定Browsable(false)，會忽略</para>
        /// </remarks>
        public static IList<GridColumnDefinition<T>> GenerateDefaultColumns<T>(this Type type)
        {
            var result = new List<GridColumnDefinition<T>>();

            // 取得所有公開屬性
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                // 檢查是否有 Browsable(false) 屬性，若有則跳過
                BrowsableAttribute? browsableAttr = prop.GetCustomAttribute<BrowsableAttribute>();

                if (browsableAttr != null && !browsableAttr.Browsable)
                    continue;

                // 嘗試讀取 DisplayName 作為 HeaderText，沒有則用屬性名稱
                var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
                string headerText = displayNameAttr != null ? displayNameAttr.DisplayName : prop.Name;

                GridColumnDefinition<T> definition = new GridColumnDefinition<T>
                {
                    ColumnName = prop.Name,
                    ColumnValue = string.Empty,
                    HeaderText = headerText,
                    Width = 100,
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    IsButtonColumn = false,
                    ButtonClickAction = null,
                    ReadOnly = false
                };
                result.Add(definition);
            }

            return result;
        }

        /// <summary>
        /// 載入Grid預設視窗設定
        /// </summary>
        /// <param name="thisForm"></param>
        public static void LoadGridFormSetting(this Form thisForm){
            thisForm.AutoSize = true;
            thisForm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            // 設定視窗在父視窗/擁有者視窗的中央開啟
            thisForm.StartPosition = FormStartPosition.CenterParent;
        }
    }

    public partial class Grid<T> : UserControl where T : class
    {
        private DataGridView _DGV;
        private IList<T> _DataSource;
        private IList<PropertyInfo> _Properties;
        private GridEditMode _EditMode = GridEditMode.ReadOnly;
        /// <summary>
        /// 用來保存「欄位順序」的清單
        /// </summary>
        private IList<GridColumnDefinition<T>> _ColumnDefinitions;
        /// <summary>
        /// 用來快速查找欄位名稱對應的 PropertyInfo
        /// </summary>
        private Dictionary<string, PropertyInfo> _PropertyCache;
        /// <summary>
        /// 用來快速查找欄位名稱對應的欄位定義
        /// </summary>
        private Dictionary<string, GridColumnDefinition<T>> _ColumnDefinitionCache;
        /// <summary>
        /// 用於暫存正在新增列中編輯的值
        /// </summary>
        /// <remarks>Key: 欄位名稱 (ColumnName)，Value: 儲存格的值</remarks>
        private Dictionary<string, object?> _NewRowBuffer = new Dictionary<string, object?>();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<T> DataSource { get { return _DataSource; } set { LoadData(value); } }

        public Grid(IList<T> datas, IList<GridColumnDefinition<T>>? columnDefinitions = null, GridEditMode editMode = GridEditMode.ReadOnly)
        {
            InitializeData(datas, columnDefinitions, editMode);

            InitializeDGV();
            
            InitializeControl();

            LoadDGVColumn();

            LoadData(null);
        }

        /// <summary>
        ///  重新載入資料
        /// </summary>
        /// <param name="datas"></param>
        public void LoadData(IList<T>? datas)
        {
            _DataSource = datas ?? (_DataSource ?? new List<T>());

            if (_DGV != null)
            {
                if (_EditMode == GridEditMode.Editable)
                {
                    // 可編輯模式多一列給使用者新增
                    //_DGV.RowCount = 1;
                    _DGV.RowCount = _DataSource.Count + 1;
                }
                else
                {
                    // 先設為 0，強迫 DGV 重置內部 ScrollBar 狀態
                    _DGV.RowCount = 0;
                    // 再設定新的數量
                    _DGV.RowCount = _DataSource.Count;
                }

                // 強制重繪 (雖然 RowCount 變更通常會觸發，但在 VirtualMode 顯式呼叫比較保險)
                _DGV.Invalidate();
            }

            AutoSizeToContent();
        }

        /// <summary>
        /// 根據目前的欄位與資料列，自動調整 UserControl 的大小
        /// </summary>
        /// <param name="maxHeight">最大允許高度</param>
        public void AutoSizeToContent(int maxHeight = 600, int maxWidth = 800)
        {
            if (_DGV == null) return;

            // Step.1 計算總寬度 (修正版)
            int totalWidth = 0;

            foreach (DataGridViewColumn col in _DGV.Columns)
                if (col.Visible)
                    totalWidth += col.Width;

            // 加上 RowHeaders 寬度 (最左邊那塊灰色選取區)
            if (_DGV.RowHeadersVisible)
                totalWidth += _DGV.RowHeadersWidth;

            // 加上垂直捲軸寬度預留 (避免內容剛好被捲軸蓋住)
            totalWidth += SystemInformation.VerticalScrollBarWidth + 4;

            // 設定一個最小寬度，避免完全消失
            if (totalWidth < 50) totalWidth = 50;

            // Step.2 計算總高度
            int totalHeight = _DGV.ColumnHeadersHeight;

            if (_DGV.RowCount > 0)
            {
                // VirtualMode 下 RowTemplate.Height 才是可靠的
                int rowHeight = _DGV.RowTemplate.Height;
                totalHeight += rowHeight * _DGV.RowCount;
            }

            // 加上水平捲軸高度預留
            totalHeight += SystemInformation.HorizontalScrollBarHeight + 4;

            // Step.3 限制最大Size
            if (totalHeight > maxHeight) totalHeight = maxHeight;
            if(totalWidth > maxWidth) totalWidth = maxWidth;

            // 設定一個最小高度
            if (totalHeight < 50) totalHeight = 50;

            // Step.4 設定 UserControl 大小
            this.Size = new Size(totalWidth, totalHeight);
        }

        /// <summary>
        /// Grid 控制項初始化
        /// </summary>
        private void InitializeControl()
        {
            InitializeComponent();
            this.Controls.Add(_DGV);
        }

        /// <summary>
        /// 初始化資料
        /// </summary>
        /// <param name="datas">Grid DataSource</param>
        /// <param name="columnDefinitions">欄位定義</param>
        /// <param name="editMode">呈現模式</param>
        private void InitializeData(IList<T> datas, IList<GridColumnDefinition<T>>? columnDefinitions, GridEditMode editMode)
        {
            _DataSource = datas;
            _Properties = typeof(T).GetProperties();
            _ColumnDefinitions = columnDefinitions ?? typeof(T).GenerateDefaultColumns<T>();

            // 建立欄位定義快取， CellValueNeeded 、 CellContentClick 不用一直重跑迴圈
            // 將 List 轉為 Dictionary，Key 是欄位名稱 (ColumnName)
            _ColumnDefinitionCache = _ColumnDefinitions.ToDictionary(c => c.ColumnName);

            // 預先抓出 T 所有的屬性，並且只保留我們有定義在 ColumnDefinitions 裡的
            _PropertyCache = _Properties
                .Where(p => _ColumnDefinitionCache.ContainsKey(p.Name))
                .ToDictionary(p => p.Name);

            _EditMode = editMode;
        }

        /// <summary>
        /// 初始化DGV
        /// </summary>
        private void InitializeDGV()
        {
            _DGV = new DataGridView();

            // 強制開啟雙緩衝，解決閃爍問題
            typeof(DataGridView).InvokeMember(
               "DoubleBuffered",
               BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
               null, _DGV, new object[] { true });

            _DGV.Dock = DockStyle.Fill;
            // 欄位標題置中
            _DGV.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            // ==========VirtualMode設定==========
            // 設定DataGridViewAutoSizeColumnsMode.AllCells會導致讀取非常慢
            _DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _DGV.DataBindings.Clear();
            _DGV.AutoGenerateColumns = false;
            _DGV.DataSource = null;
            _DGV.VirtualMode = true;
            _DGV.CellValueNeeded += DGV_CellValueNeeded;
            _DGV.CellContentClick += DGV_CellContentClick;
            // ==========VirtualMode設定==========

            // ==========處理 DataError===========
            _DGV.DataError += (s, e) =>
            {
                // 這裡可以 log e.Exception
                e.ThrowException = false;
                e.Cancel = false;
            };
            // ==========處理 DataError===========

            if (_EditMode == GridEditMode.Editable)
            {
                _DGV.ReadOnly = false;
                // _DGV.RowCount 會比 _DataSource.Count 多 1（用於新增資料列）
                // 編輯最後一列（即新增列） 時，e.RowIndex 會等於 _DataSource.Count
                _DGV.AllowUserToAddRows = true;
                // 寫入事件
                _DGV.CellValuePushed += DGV_CellValuePushed;
            }
            else
            {
                _DGV.ReadOnly = true;
                // 避免最後一列消失
                _DGV.AllowUserToAddRows = false;
            }
        }

        /// <summary>
        /// 載入欄位、設定樣式
        /// </summary>
        private void LoadDGVColumn()
        {
            if (_DGV is null) InitializeDGV();

            DataGridViewColumnCollection? columns = _DGV?.Columns;

            // 加入缺少的欄位
            foreach (GridColumnDefinition<T> definition in _ColumnDefinitions)
            {
                if (!columns?.Contains(definition.ColumnName) ?? false)
                {
                    if (definition.IsButtonColumn)
                    {
                        // 建立按鈕欄位
                        DataGridViewButtonColumn btn = new DataGridViewButtonColumn
                        {
                            Name = definition.ColumnName,
                            HeaderText = definition.ColumnName,
                            UseColumnTextForButtonValue = false
                        };
                        columns?.Add(btn);
                    }
                    else
                    {
                        // 一般文字欄位
                        DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn
                        {
                            Name = definition.ColumnName,
                            HeaderText = definition.ColumnName
                        };
                        columns?.Add(col);
                    }
                }
            }

            // 依照 ColumnOrderAndHeader 控制可見/排序
            int displayIndex = 0;
            HashSet<string> visibleFields = new HashSet<string>(
                _ColumnDefinitionCache.Values.Select(c => c.ColumnName),
                StringComparer.OrdinalIgnoreCase
            );

            // 隱藏非白名單欄位
            foreach (DataGridViewColumn column in columns)
                column.Visible = visibleFields.Contains(column.Name);

            // 定義欄位、設定排序與標題
            foreach (GridColumnDefinition<T> definition in _ColumnDefinitions)
            {
                if (columns.Contains(definition.ColumnName))
                {
                    columns[definition.ColumnName].Visible = true;
                    columns[definition.ColumnName].HeaderText = definition.HeaderText;
                    columns[definition.ColumnName].Width = definition.Width;
                    columns[definition.ColumnName].DefaultCellStyle.Alignment = definition.Alignment;
                    columns[definition.ColumnName].DisplayIndex = displayIndex++;
                    // ToDo:按鈕欄位 (ButtonColumn) 通常保持 ReadOnly = true，除非你要改按鈕上的字
                    columns[definition.ColumnName].ReadOnly = definition.ReadOnly;
                }
            }
        }

        /// <summary>
        /// DataGridView 儲存格載入事件
        /// </summary>
        /// <remarks>
        /// 當 DGV 需要某個儲存格的資料時觸發
        /// </remarks>
        /// <param name="sender">觸發事件的 DataGridView 控制項</param>
        /// <param name="e">包含行索引 RowIndex 和列索引 ColumnIndex 的事件參數</param>
        private void DGV_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            // 如果資料還沒載入，或索引錯誤，直接跳出
            if (_DataSource == null || e.RowIndex >= _DataSource.Count || e.RowIndex < 0) return;

            // 當前格子對應的欄位名稱
            string colName = _DGV.Columns[e.ColumnIndex].Name;

            // 處理新增資料列的顯示
            if (_EditMode == GridEditMode.Editable && e.RowIndex == _DataSource.Count)
            {
                // 如果暫存區有值，就顯示
                if (_NewRowBuffer.TryGetValue(colName, out object? tempValue))
                    e.Value = tempValue;
                return;
            }

            // 如果資料還沒載入，或索引錯誤，直接跳出 (原本的判斷邏輯)
            if (e.RowIndex >= _DataSource.Count) return;

            // 當前這列的資料物件
            T dataItem = _DataSource[e.RowIndex];

            // Step.1 檢查是不是實體屬性 (Property)
            // TryGetValue 非常快，如果字典裡有這個 colName，就會把屬性丟給 prop 變數
            if (_PropertyCache.TryGetValue(colName, out PropertyInfo? prop))
            {
                e.Value = prop.GetValue(dataItem);
                return;
            }

            // Step.2 檢查是不是自訂欄位
            if (_ColumnDefinitionCache.TryGetValue(colName, out GridColumnDefinition<T>? def))
            {
                // 如果這個欄位有設定固定的顯示值
                if (!string.IsNullOrEmpty(def.ColumnValue))
                {
                    e.Value = def.ColumnValue;
                    return;
                }
            }
        }

        /// <summary>
        /// DataGridView 儲存格內容點擊事件
        /// </summary>
        /// <remarks>用於處理使用者點擊按鈕欄位 Button Columns 的操作</remarks>
        /// <param name="sender">觸發事件的 DataGridView 控制項</param>
        /// <param name="e">包含點擊位置的行索引 RowIndex 和列索引 ColumnIndex 的事件參數</param>
        private void DGV_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if ((e.RowIndex < 0 || e.ColumnIndex < 0) || 
                (e.RowIndex > (_DataSource.Count - 1))) 
                return;

            string colName = _DGV.Columns[e.ColumnIndex].Name;

            if (_ColumnDefinitionCache.TryGetValue(colName, out GridColumnDefinition<T>? definition))
                if (definition.IsButtonColumn && definition.ButtonClickAction != null && _DataSource[e.RowIndex] != null)
                    definition.ButtonClickAction.Invoke(_DataSource[e.RowIndex]);
        }

        /// <summary>
        /// DataGridView 儲存格值寫入事件
        /// </summary>
        /// <remarks>當使用者編輯儲存格並提交變更時觸發 (寫入資料)</remarks>
        /// <param name="sender">觸發事件的 DataGridView 控制項</param>
        /// <param name="e">包含行索引 RowIndex 和列索引 ColumnIndex 的事件參數</param>
        private void DGV_CellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (_DataSource == null || e.RowIndex < 0) return;

            string colName = _DGV.Columns[e.ColumnIndex].Name;

            // 判斷是否為「新增資料列」
            // 編輯最後一列（即新增列） 時，e.RowIndex 會等於 _DataSource.Count
            if (e.RowIndex == _DataSource.Count)
            {
                HandleNewRowInput(e, colName);
                return;
            }

            // 處理「編輯現有資料」
            if (e.RowIndex < _DataSource.Count)
            {
                T dataItem = _DataSource[e.RowIndex];

                // 只有針對實體屬性 (Property) 進行寫回 (其餘邏輯不變)
                if (_PropertyCache.TryGetValue(colName, out PropertyInfo? prop))
                {
                    try
                    {
                        object? newValue = ConvertToPropertyType(e.Value, prop.PropertyType);
                        prop.SetValue(dataItem, newValue);
                    }
                    catch (Exception ex)
                    {
                        // 可以考慮不彈 MessageBox，而是在 DGV.DataError 中處理
                        MessageBox.Show($"輸入格式錯誤: {ex.Message}");
                    }
                }

                // 由於資料已修改，需要通知 DGV 重繪該列
                _DGV.InvalidateRow(e.RowIndex);
            }
        }

        /// <summary>
        /// 處理新增資料列 (最後一列) 的輸入
        /// </summary>
        private void HandleNewRowInput(DataGridViewCellValueEventArgs e, string colName)
        {
            // 暫存輸入的值
            _NewRowBuffer[colName] = e.Value;

            AddNewRowFromBuffer();
        }

        /// <summary>
        /// 根據暫存區的資料，建立新的 T 物件並加入 DataSource
        /// </summary>
        private void AddNewRowFromBuffer()
        {
            // Step.1 建立 T 的新實例 (需要 T 具有無參數建構子)
            T? newObject = Activator.CreateInstance<T>();
            if (newObject == null) return;

            // Step.2 將暫存區的值寫入新物件
            foreach (KeyValuePair<string, object?> entry in _NewRowBuffer)
            {
                string propName = entry.Key;
                object? inputValue = entry.Value;

                if (_PropertyCache.TryGetValue(propName, out PropertyInfo? prop))
                {
                    try
                    {
                        object? typedValue = ConvertToPropertyType(inputValue, prop.PropertyType);
                        prop.SetValue(newObject, typedValue);
                    }
                    catch (Exception ex)
                    {
                        // 忽略格式錯誤的值，或者給予預設值
                        Debug.WriteLine($"New row property set error: {propName}, {ex.Message}");
                    }
                }
            }

            // Step.3 加入 DataSource
            _DataSource.Add(newObject);

            // Step.4 清空暫存區，為下一筆新增做準備
            _NewRowBuffer.Clear();

            // Step.5 重新載入 DGV (更新 RowCount，確保新增列再次出現)
            LoadData(null);

            // Step.6 讓 DGV 滾動到新增的那一列
            if (_DGV.RowCount > 0)
                _DGV.FirstDisplayedScrollingRowIndex = _DGV.RowCount - 1;
        }

        /// <summary>
        /// 通用型別轉換
        /// </summary>
        private object? ConvertToPropertyType(object? value, Type targetType)
        {
            if (value == null || value == DBNull.Value) return null;

            // 處理 Nullable<T>，例如 int?
            Type? underlyingType = Nullable.GetUnderlyingType(targetType);
            Type finalType = underlyingType ?? targetType;

            // 如果輸入是空字串且目標是 Nullable，則回傳 null
            if (underlyingType != null && string.IsNullOrWhiteSpace(value.ToString()))
                return null;

            // 進行轉換
            return Convert.ChangeType(value, finalType);
        }
    }
}
