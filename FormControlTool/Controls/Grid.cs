using System.Reflection;

namespace SeanTool.CSharp.Net8.Forms
{
    public class GridColumnDefinition<T>
    {
        public string ColumnName { get; set; } = string.Empty;
        public string ColumnValue { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public int Width { get; set; }
        public DataGridViewContentAlignment Locate { get; set; }
        public bool IsButtonColumn { get; set; } = false;
        public Action<T>? ButtonClickAction { get; set; }
    }

    public partial class Grid<T> : UserControl
    {
        private DataGridView _DGV;

        private IList<T> _Datas { get; set; }

        private IList<PropertyInfo> _Properties { get; set; }

        public IList<GridColumnDefinition<T>> ColumnDefinitions { get; set; }

        public Grid(IList<T> datas)
        {
            _Datas = datas; _Datas = datas;
            _Properties = typeof(T).GetProperties().ToList();

            InitializeComponent();
            this.Dock = DockStyle.Fill;

            ColumnDefinitions = GenDefaultGridColumnDefinition();

            InitializeDGV();
            LoadDGVColumn();
        }

        public void InitializeDGV()
        {
            _DGV = new DataGridView();

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

            if (_Datas != null)
                _DGV.RowCount = _Datas.Count;

            this.Controls.Clear();
            this.Controls.Add(_DGV);
        }

        /// <summary>
        /// DGVMode Event
        /// </summary>
        /// <remarks>
        /// 當 DGV 需要某個儲存格的資料時觸發
        /// </remarks>
        /// <param name="sender">觸發事件的 DataGridView 控制項</param>
        /// <param name="e">包含行索引 RowIndex 和列索引 ColumnIndex 的事件參數</param>
        protected void DGV_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            PropertyInfo? prop =
                _Properties.Where(p => p.Name == _DGV.Columns[e.ColumnIndex].Name).FirstOrDefault();

            // 先檢查Data屬性值
            if (prop != null){
                object? value = prop.GetValue(_Datas[e.RowIndex]);
                e.Value = value;

                return;
            }

            // 再檢查自訂欄位值(按鈕)
            GridColumnDefinition<T> definition =
                ColumnDefinitions.Where(c => c.ColumnName == _DGV.Columns[e.ColumnIndex].Name && !string.IsNullOrEmpty(c.ColumnValue))
                .FirstOrDefault()!;

            if(definition != null){
                e.Value = definition.ColumnValue;

                return;
            }
        }

        /// <summary>
        /// DataGridView 儲存格內容點擊事件
        /// </summary>
        /// <remarks>用於處理使用者點擊按鈕欄位 Button Columns 的操作</remarks>
        /// <param name="sender">觸發事件的 DataGridView 控制項</param>
        /// <param name="e">包含點擊位置的行索引 RowIndex 和列索引 ColumnIndex 的事件參數</param>
        protected void DGV_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            GridColumnDefinition<T> definition = 
                ColumnDefinitions.Where(c => c.ColumnName == _DGV.Columns[e.ColumnIndex].Name && c.ButtonClickAction != null)
                .FirstOrDefault()!;

            if(definition != null && definition.ButtonClickAction != null)
                definition.ButtonClickAction.Invoke(_Datas[e.RowIndex]);
        }

        public IList<GridColumnDefinition<T>> GenDefaultGridColumnDefinition()
        {
            IList<GridColumnDefinition<T>> result = new List<GridColumnDefinition<T>>();

            foreach(PropertyInfo prop in _Properties)
            {
                GridColumnDefinition<T> definition = new GridColumnDefinition<T>
                {
                    ColumnName = prop.Name,
                    ColumnValue = string.Empty,
                    HeaderText = prop.Name,
                    Width = 100,
                    Locate = DataGridViewContentAlignment.MiddleLeft,
                    IsButtonColumn = false,
                    ButtonClickAction = null
                };
                result.Add(definition);
            }

            return result;
        }

        /// <summary>
        /// 載入欄位、設定樣式
        /// </summary>
        public void LoadDGVColumn()
        {
            if (ColumnDefinitions is null) 
                throw new InvalidOperationException("請先設定 ColumnDefinitions");

            if (_DGV is null) InitializeDGV();

            DataGridViewColumnCollection columns = _DGV.Columns;

            // 加入缺少的欄位
            foreach (GridColumnDefinition<T> definition in ColumnDefinitions)
            {
                if (!columns.Contains(definition.ColumnName))
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
                        columns.Add(btn);
                    }
                    else
                    {
                        // 一般文字欄位
                        DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn
                        {
                            Name = definition.ColumnName,
                            HeaderText = definition.ColumnName
                        };
                        columns.Add(col);
                    }
                }
            }

            // 依照 ColumnOrderAndHeader 控制可見/排序
            int displayIndex = 0;
            HashSet<string> visibleFields = new HashSet<string>(
                ColumnDefinitions.Select(c => c.ColumnName),
                StringComparer.OrdinalIgnoreCase
            );

            // 隱藏非白名單欄位
            foreach (DataGridViewColumn column in columns)
                column.Visible = visibleFields.Contains(column.Name);

            // 設定排序與標題
            foreach (GridColumnDefinition<T> definition in ColumnDefinitions)
            {
                if (columns.Contains(definition.ColumnName))
                {
                    columns[definition.ColumnName].Visible = true;
                    columns[definition.ColumnName].HeaderText = definition.HeaderText;
                    columns[definition.ColumnName].Width = definition.Width;
                    columns[definition.ColumnName].DefaultCellStyle.Alignment = definition.Locate;
                    columns[definition.ColumnName].DisplayIndex = displayIndex++;
                }
            }
        }
    }
}
