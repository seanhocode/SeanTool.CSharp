using System.Reflection;

namespace SeanTool.CSharp.Net8.Forms
{
    /* ToDo
     * 1. SortCompare
     * 2. Filter
     * 3. Paging
     * 4. Edit
     */
    public class GridColumnDefinition<T>
    {
        public string ColumnName { get; set; } = string.Empty;
        public string ColumnValue { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public int Width { get; set; }
        public DataGridViewContentAlignment Alignment { get; set; }
        public bool IsButtonColumn { get; set; } = false;
        public Action<T>? ButtonClickAction { get; set; }
    }

    public partial class Grid<T> : UserControl
    {
        private DataGridView _DGV;

        private IList<T> _DataSource { get; set; }

        private IList<PropertyInfo> _Properties { get; set; }

        // [新增] 用來保存「欄位順序」的清單
        private IList<GridColumnDefinition<T>> _ColumnDefinitions;

        // 用來快速查找欄位名稱對應的 PropertyInfo
        private Dictionary<string, PropertyInfo> _PropertyCache;

        // 用來快速查找欄位名稱對應的欄位定義
        private Dictionary<string, GridColumnDefinition<T>> _ColumnDefinitionCache;

        public IList<T> DataSource { get { return _DataSource; } set { LoadData(value); } }

        public Grid(IList<T> datas, IList<GridColumnDefinition<T>>? columnDefinitions = null)
        {
            InitializeData(datas, columnDefinitions);

            InitializeDGV();
            
            InitializeControl();

            LoadDGVColumn();

            LoadData(null);
        }

        public IList<GridColumnDefinition<T>> GenDefaultGridColumnDefinition()
        {
            IList<GridColumnDefinition<T>> result = new List<GridColumnDefinition<T>>();

            foreach (PropertyInfo prop in _Properties)
            {
                GridColumnDefinition<T> definition = new GridColumnDefinition<T>
                {
                    ColumnName = prop.Name,
                    ColumnValue = string.Empty,
                    HeaderText = prop.Name,
                    Width = 100,
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    IsButtonColumn = false,
                    ButtonClickAction = null
                };
                result.Add(definition);
            }

            return result;
        }

        private void InitializeControl()
        {
            InitializeComponent();

            this.Dock = DockStyle.Fill;

            this.Controls.Add(_DGV);
        }

        private void InitializeData(IList<T> datas, IList<GridColumnDefinition<T>>? columnDefinitions)
        {
            _DataSource = datas;
            _Properties = typeof(T).GetProperties();
            _ColumnDefinitions = columnDefinitions ?? GenDefaultGridColumnDefinition();

            // 建立欄位定義快取， CellValueNeeded 、 CellContentClick 不用一直重跑迴圈
            // 將 List 轉為 Dictionary，Key 是欄位名稱 (ColumnName)
            _ColumnDefinitionCache = _ColumnDefinitions.ToDictionary(c => c.ColumnName);

            // 預先抓出 T 所有的屬性，並且只保留我們有定義在 ColumnDefinitions 裡的
            _PropertyCache = _Properties
                .Where(p => _ColumnDefinitionCache.ContainsKey(p.Name))
                .ToDictionary(p => p.Name);
        }

        private void InitializeDGV()
        {
            _DGV = new DataGridView();

            // 強制開啟雙緩衝，解決閃爍問題
            typeof(DataGridView).InvokeMember(
               "DoubleBuffered",
               BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
               null, _DGV, new object[] { true });

            _DGV.Dock = DockStyle.Fill;
            // 設定DataGridViewAutoSizeColumnsMode.AllCells會導致讀取非常慢
            _DGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            // 避免最後一列消失
            _DGV.AllowUserToAddRows = false;
            // 欄位標題置中
            _DGV.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            // ==========VirtualMode設定==========
            _DGV.DataBindings.Clear();
            _DGV.AutoGenerateColumns = false;
            _DGV.DataSource = null;
            _DGV.VirtualMode = true;
            _DGV.CellValueNeeded += DGV_CellValueNeeded;
            _DGV.CellContentClick += DGV_CellContentClick;
            // ==========VirtualMode設定==========
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

            // 設定排序與標題
            foreach (GridColumnDefinition<T> definition in _ColumnDefinitions)
            {
                if (columns.Contains(definition.ColumnName))
                {
                    columns[definition.ColumnName].Visible = true;
                    columns[definition.ColumnName].HeaderText = definition.HeaderText;
                    columns[definition.ColumnName].Width = definition.Width;
                    columns[definition.ColumnName].DefaultCellStyle.Alignment = definition.Alignment;
                    columns[definition.ColumnName].DisplayIndex = displayIndex++;
                }
            }
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
                // 先設為 0，強迫 DGV 重置內部 ScrollBar 狀態
                _DGV.RowCount = 0;

                // 再設定新的數量
                _DGV.RowCount = _DataSource.Count;

                // 強制重繪 (雖然 RowCount 變更通常會觸發，但在 VirtualMode 顯式呼叫比較保險)
                _DGV.Invalidate();
            }
        }

        /// <summary>
        /// DGVMode Event
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
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = _DGV.Columns[e.ColumnIndex].Name;

            if (_ColumnDefinitionCache.TryGetValue(colName, out GridColumnDefinition<T>? definition))
                if (definition.IsButtonColumn && definition.ButtonClickAction != null)
                    definition.ButtonClickAction.Invoke(_DataSource[e.RowIndex]);
        }
    }
}
