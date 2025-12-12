using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Transactions;

namespace SeanTool.CSharp.Net8
{
    # region Custom Attributes
    /// <summary>
    /// TableName 屬性，用於指定Model對應的資料表名稱
    /// </summary>
    public class TableNameAttribute : Attribute
    {
        public string TableName { get; }

        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }

    /// <summary>
    /// PrimaryKey 屬性，用於標記Model的主鍵屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]// 限制只能貼在屬性上
    public class PrimaryKeyAttribute : Attribute { }

    /// <summary>
    /// Identity 屬性，用於標記Model的自動增量屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]// 限制只能貼在屬性上
    public class IdentityAttribute : Attribute { }

    /// <summary>
    /// VarChar 屬性，用於標記Model的字串屬性應對應到資料庫的 VARCHAR 類型
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]// 限制只能貼在屬性上
    public class VarCharAttribute : Attribute { }
    # endregion

    /// <summary>
    /// SqlTool 擴充方法
    /// </summary>
    /// <remarks></remarks>
    public static class SqlToolExtensions
    {
        /// <summary>
        /// 擴充 IServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connectionString"></param>
        /// <returns>使用方式:於Program.cs => builder.Services.AddSqlTool([connStr]);</returns>
        public static IServiceCollection AddSqlTool(this IServiceCollection services, string connectionString)
        {
            // Scoped 服務會在每次 HTTP 請求進來時建立一個新的實體，並在請求結束時自動釋放
            services.AddScoped<ISqlTool>(sp => new SqlTool(connectionString));
            return services;
        }
    }

    public class SqlTool : ISqlTool
    {
        # region VAR
        /// <summary>
        /// Model Metadata
        /// </summary>
        private class ModelMetadata
        {
            /// <summary>
            /// 取得資料表名稱 by TableNameAttribute
            /// </summary>
            /// <returns>TableNameAttribute設定的TableName，若無設定則回傳ModelName</returns>
            public string TableName { get; }

            /// <summary>
            /// Model所有屬性清單
            /// </summary>
            public IList<PropertyInfo> AllProperties { get; }

            /// <summary>
            /// Insert可用的屬性清單
            /// </summary>
            /// <remarks>忽略有設定IdentityAttribute的屬性</remarks>
            public IList<PropertyInfo> InsertProperties { get; }

            /// <summary>
            /// Update可更新的屬性清單
            /// </summary>
            /// <remarks>忽略有設定PrimaryKeyAttribute及IdentityAttribute的屬性</remarks>
            public IList<PropertyInfo> UpdateProperties { get; }

            /// <summary>
            /// PrimaryKey屬性清單 by PrimaryKeyAttribute
            /// </summary>
            /// <returns>PrimaryKey屬性清單</returns>
            public IList<PropertyInfo> KeyProperties { get; }

            public IList<PropertyInfo> VarCharProperties { get; }

            /// <summary>
            /// 建構子，解析Model的Metadata
            /// </summary>
            /// <param name="type"></param>
            public ModelMetadata(Type type)
            {
                TableNameAttribute? attr = 
                    type.GetCustomAttributes(typeof(TableNameAttribute), true).FirstOrDefault() as TableNameAttribute;
                TableName = attr?.TableName ?? type.Name;

                AllProperties = type.GetProperties();

                InsertProperties = AllProperties
                    .Where(p => p.GetCustomAttribute<IdentityAttribute>() == null)
                    .ToList();

                UpdateProperties = AllProperties
                    .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>() == null
                             && p.GetCustomAttribute<IdentityAttribute>() == null)
                    .ToList();

                KeyProperties = AllProperties
                    .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                    .ToList();

                VarCharProperties =  AllProperties
                    .Where(p => p.GetCustomAttribute<VarCharAttribute>() != null)
                    .ToList();
            }
        }

        /// <summary>
        /// Model Metadata快取
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ModelMetadata> _MetadataCache = new();

        /// <summary>
        /// 連線字串
        /// </summary>
        private readonly string _ConnectionString;

        /* 暫存目前正在使用的連線
         * 放在物件變數，確保在同一次 HTTP 請求 (Request) 中，拿到同一個連線
         * 在手動開啟共用交易模式下需要指定 SqlConnection.Transaction
         */
        private SqlConnection? _SharedConn;
        private SqlTransaction? _SharedTrans;

        /* TransactionScope 決定了哪些資料庫操作要被視為同一件事
         * TransactionScope 模式下不需要指定 SqlConnection.Transaction，ADO.NET 會自動處理
         * 如果其中一個步驟失敗了，在這個範圍內的所有操作都會一起 Rollback
         * TransactionScope 工作時預設使用的是輕量級交易(LTM)
         * 在同一個 TransactionScope 內，同時使用了兩條以上的連線，可能會試圖啟動 MSDTC(分散式交易協調器) 服務
         * 一旦變成 MSDTC：
         *      效能變差：速度會慢很多
         *      容易報錯：如果資料庫伺服器或是電腦沒有開啟 MSDTC 服務（通常預設關閉），程式會噴錯
         */
        private TransactionScope? _CurrentScope;
        # endregion

        public SqlTool(string connStr)
        {
            _ConnectionString = connStr;
        }

        # region Core
        #region SQLTransaction
        /* 如果預設全部都共用模式，會違反 .NET 資料庫開發原則：晚開早關 (Open Late, Close Early)
         * 開啟共用模式時，資料庫連線會直被佔用，佔用時間過長，會拖垮伺服器
         * 且會有狀態污染 (Side Effects) 問題
         * 手動控制共用連線，充分利用 .NET 的 Connection Pool，且確保每次使用的連線都是乾淨的(用完即丟(Stateless)模式)
        */
        public void OpenSharedConnection()
        {
            if (_SharedConn == null)
            {
                _SharedConn = new SqlConnection(_ConnectionString);
                _SharedConn.Open();
            }
            else if (_SharedConn.State != ConnectionState.Open)
            {
                _SharedConn.Open();
            }
        }

        public async Task OpenSharedConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_SharedConn == null)
            {
                _SharedConn = new SqlConnection(_ConnectionString);
                await _SharedConn.OpenAsync(cancellationToken);
            }
            else if (_SharedConn.State != ConnectionState.Open)
            {
                await _SharedConn.OpenAsync(cancellationToken);
            }
        }

        public void BeginTransaction()
        {
            if (_CurrentScope != null)
                throw new InvalidOperationException("已在 TransactionScope 模式中，不可混合使用手動 SqlTransaction。");

            OpenSharedConnection();

            if (_SharedTrans == null)
                _SharedTrans = _SharedConn!.BeginTransaction();
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_CurrentScope != null)
                throw new InvalidOperationException("已在 TransactionScope 模式中，不可混合使用手動 SqlTransaction。");

            await OpenSharedConnectionAsync(cancellationToken);

            if (_SharedTrans == null)
                _SharedTrans = _SharedConn!.BeginTransaction();
        }

        public void Commit()
        {
            _SharedTrans?.Commit();
            _SharedTrans = null; // 清空交易物件，準備下一次
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_SharedTrans != null)
            {
                await _SharedTrans.CommitAsync(cancellationToken);
                _SharedTrans = null;
            }
        }

        public void Rollback()
        {
            _SharedTrans?.Rollback();
            _SharedTrans = null;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_SharedTrans != null)
            {
                await _SharedTrans.RollbackAsync(cancellationToken);
                _SharedTrans = null;
            }
        }
        #endregion

        #region TransactionScope
        public void StartTransactionScope(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.ReadCommitted, int timeoutSeconds = 60)
        {
            if (_SharedTrans != null)
                throw new InvalidOperationException("已在手動 SqlTransaction 模式中，不可混合使用 TransactionScope。");

            if (_SharedConn != null && _SharedConn.State == ConnectionState.Open)
                throw new InvalidOperationException("連線已開啟，無法建立 TransactionScope。請確保在開啟 Scope 前連線是關閉的，或讓 Scope 自動管理連線。");

            // Step.1 設定 TransactionOptions
            TransactionOptions options = new TransactionOptions
            {
                IsolationLevel = level,
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            // Step.2 建立 Scope
            // TransactionScopeAsyncFlowOption.Enabled 是為了支援 async/await
            _CurrentScope = new TransactionScope(TransactionScopeOption.Required, options, TransactionScopeAsyncFlowOption.Enabled);

            // Step.3 Scope 建立後，立刻開啟共用連線
            // 這條連線會自動加入這個 Scope，避免自動開啟多條連線導致 MSDTC
            OpenSharedConnection();
        }

        public void CommitScope()
        {
            _CurrentScope?.Complete();
        }
        #endregion

        # region private methods
        /// <summary>
        /// 取得Model Metadata
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <remarks>沒有則新增</remarks>
        /// <returns></returns>
        private ModelMetadata GetModelMetadata<T>()
        {
            return GetModelMetadata(typeof(T));
        }

        /// <summary>
        /// 取得Model Metadata
        /// </summary>
        /// <param name="type">Model Type</param>
        /// <remarks>沒有則新增</remarks>
        /// <returns></returns>
        private ModelMetadata GetModelMetadata(Type type)
        {
            return _MetadataCache.GetOrAdd(type, new ModelMetadata(type));
        }

        /// <summary>
        /// 檢查是否有共用連線，並取得連線物件
        /// </summary>
        /// <returns>有共用回傳現有連線，無共用則回傳薪的</returns>
        private SqlConnection GetConnection()
        {
            SqlConnection conn = _SharedConn ?? new SqlConnection(_ConnectionString);
            if (conn.State != ConnectionState.Open) conn.Open();
            return conn;
        }

        /// <summary>
        /// 非同步檢查是否有共用連線，並取得連線物件
        /// </summary>
        /// <returns>有共用回傳現有連線，無共用則回傳薪的</returns>
        private async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            SqlConnection conn = _SharedConn ?? new SqlConnection(_ConnectionString);
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken);
            return conn;
        }

        /// <summary>
        /// 建立 SqlCommand
        /// </summary>
        /// <param name="sql">SQL指令</param>
        /// <param name="conn">連線物件</param>
        /// <param name="commandType">指令類型，預設Text</param>
        /// <returns>包含交易設定的SqlCommand</returns>
        private SqlCommand CreateCommand(string sql, SqlConnection conn, CommandType commandType = CommandType.Text)
        {
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = commandType;
            // 在手動交易模式下需要指定 Transaction
            // TransactionScope 模式下，ADO.NET 會自動處理，不需要指定 cmd.Transaction
            if (_SharedTrans != null)
                cmd.Transaction = _SharedTrans;

            return cmd;
        }

        /// <summary>
        /// 關閉內部連線
        /// </summary>
        /// <remarks>僅用於非共用連線</remarks>
        /// <param name="conn">SqlConnection</param>
        private void CloseInternalConnection(SqlConnection conn)
        {
            conn.Close();
            conn.Dispose();
        }

        /// <summary>
        /// 取得參數Type對應的 SqlDbType
        /// </summary>
        /// <param name="type">參數Type</param>
        /// <returns>(SqlDbType, Size)</returns>
        private (SqlDbType Type, int Size) GetSqlDbType(Type type, bool isVarChar = false)
        {
            // 取得底層型別 (處理 Nullable<T>)
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(int)) return (SqlDbType.Int, 0);
            if (underlyingType == typeof(long)) return (SqlDbType.BigInt, 0);
            if (underlyingType == typeof(short)) return (SqlDbType.SmallInt, 0);
            if (underlyingType == typeof(byte)) return (SqlDbType.TinyInt, 0);
            if (underlyingType == typeof(bool)) return (SqlDbType.Bit, 0);
            if (underlyingType == typeof(DateTime)) return (SqlDbType.DateTime2, 0); // 建議用 DateTime2 避免 1753 年限制
            if (underlyingType == typeof(decimal)) return (SqlDbType.Decimal, 0);
            if (underlyingType == typeof(double)) return (SqlDbType.Float, 0);
            if (underlyingType == typeof(float)) return (SqlDbType.Real, 0);
            if (underlyingType == typeof(Guid)) return (SqlDbType.UniqueIdentifier, 0);
            if (underlyingType == typeof(byte[])) return (SqlDbType.VarBinary, -1); // -1 代表 MAX

            // 若 DB 為 VARCHAR，C# 傳 NVARCHAR 會導致索引失效
            // 這裡指定 VarChar 解決隱性轉型，Size 設為 -1 (MAX) 或 8000 可涵蓋大多數情況
            // 但 ADO.NET 會自動根據值調整，除非需要截斷
            if (underlyingType == typeof(string) && isVarChar) return (SqlDbType.VarChar, -1);
            if (underlyingType == typeof(string) && !isVarChar) return (SqlDbType.NVarChar, -1);

            // 預設 fallback
            return (SqlDbType.Variant, 0);
        }

        /// <summary>
        /// SqlCommand加入參數
        /// </summary>
        /// <param name="cmd">SqlCommand</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        private void AddParameters(SqlCommand cmd, object? parameters)
        {
            if (parameters == null) return;

            // 讓使用者可以直接傳入 SqlParameter 陣列 (用於 Output 參數)
            if (parameters is IEnumerable<SqlParameter> sqlParams)
            {
                foreach (SqlParameter p in sqlParams)
                    cmd.Parameters.Add(p);

                return;
            }

            if (parameters is IDictionary dict)
            {
                foreach (DictionaryEntry entry in dict)
                {
                    string key = entry.Key.ToString() ?? "";
                    if (string.IsNullOrEmpty(key)) continue;

                    object val = entry.Value ?? DBNull.Value;

                    // Dictionary 比較難推斷屬性型別，通常依賴值的型別
                    // 若 val 是 null，預設用 VarChar
                    Type valType = val != DBNull.Value ? val.GetType() : typeof(string);

                    (SqlDbType dbType, int size) = GetSqlDbType(valType);

                    // 修正 Dictionary key 需補上 @ (如果沒有的話)
                    string paramName = key.StartsWith("@") ? key : $"@{key}";

                    SqlParameter param = cmd.Parameters.Add(paramName, dbType, size);
                    param.Value = val;
                }
            }
            else
            {
                // AddWithValue在處理字串(String)時，通常會預設為 NVARCHAR(4000)。
                // 如果資料庫欄位是 VARCHAR 且有索引(Index)，這會導致 SQL Server 發生 隱式轉型(Implicit Conversion)
                // 導致索引失效，全表掃描(Full Table Scan)，效能大幅下降
                foreach (PropertyInfo prop in GetModelMetadata(parameters.GetType()).AllProperties)
                {
                    bool isVarChar = GetModelMetadata(parameters.GetType()).VarCharProperties.Contains(prop);
                    // Step.1 取得對應的 SqlDbType
                    (SqlDbType dbType, int size) = GetSqlDbType(prop.PropertyType, isVarChar);

                    // Step.2 建立參數 (明確指定型別，解決隱性轉型效能問題)
                    SqlParameter param = cmd.Parameters.Add($"@{prop.Name}", dbType, size);

                    // Step.3 設定值
                    param.Value = prop.GetValue(parameters) ?? DBNull.Value;
                }
            }
        }
        # endregion

        # region public methods
        public int ExecuteNonQuery(string sql, object? parameters = null)
        {
            SqlConnection conn = GetConnection();
            bool isInternalConn = _SharedConn == null;

            try
            {
                using (SqlCommand cmd = CreateCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            SqlConnection conn = await GetConnectionAsync(cancellationToken);
            bool isInternalConn = _SharedConn == null;

            try
            {
                using (SqlCommand cmd = CreateCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    return await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public object? ExecuteScalar(string sql, object? parameters = null)
        {
            SqlConnection conn = GetConnection();
            bool isInternalConn = _SharedConn == null;

            try
            {
                using (SqlCommand cmd = CreateCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteScalar();
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public async Task<object?> ExecuteScalarAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            SqlConnection conn = await GetConnectionAsync(cancellationToken);
            bool isInternalConn = _SharedConn == null;
            try
            {
                using (SqlCommand cmd = CreateCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    return await cmd.ExecuteScalarAsync(cancellationToken);
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public DataTable ExecuteSQL(string sql, object? parameters = null)
        {
            SqlConnection conn = GetConnection();
            bool isInternalConn = _SharedConn == null;

            try
            {
                using (SqlCommand cmd = CreateCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public async Task<DataTable> ExecuteSQLAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            SqlConnection conn = await GetConnectionAsync();
            bool isInternalConn = _SharedConn == null;

            try
            {
                using (SqlCommand cmd = CreateCommand(sql, conn))
                {
                    AddParameters(cmd, parameters);

                    // 使用 ExecuteReaderAsync
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        DataTable dt = new DataTable();

                        // 這裡使用 dt.Load(reader) 
                        // 但 dt.Load 本身是同步的。如果要純非同步，需手動寫迴圈讀取
                        // 實務上在 .NET 8，為了方便通常接受 dt.Load(reader) 的短暫 CPU 阻塞，
                        // 或者寫一個 Helper 使用 await reader.ReadAsync() 來填充
                        dt.Load(reader);
                        return dt;
                    }
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        /// <summary>
        /// 實作 IDisposable，確保共用連線最後會被關閉
        /// </summary>
        /// <remarks>
        /// <para>依序清除</para>
        /// <list type="number">
        /// <item>SqlTransaction</item>
        /// <item>SqlConnection</item>
        /// <item>TransactionScope</item>
        /// </list>
        /// </remarks>
        public void Dispose()
        {
            // Step.1 清理 SqlTransaction
            if (_SharedTrans != null)
            {
                _SharedTrans.Dispose();
                _SharedTrans = null;
            }

            // Step.2 清理連線 (通常建議先關連線，再關 Scope，或者讓 Scope 自動退回)
            // 先明確關閉連線
            if (_SharedConn != null)
            {
                if (_SharedConn.State == ConnectionState.Open)
                    _SharedConn.Close();
                _SharedConn.Dispose();
                _SharedConn = null;
            }

            // Step.3 清理 TransactionScope
            if (_CurrentScope != null)
            {
                _CurrentScope.Dispose();
                _CurrentScope = null;
            }

            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            // Step.1 清理 SqlTransaction
            if (_SharedTrans != null)
            {
                await _SharedTrans.DisposeAsync();
                _SharedTrans = null;
            }

            // Step.2 清理連線
            if (_SharedConn != null)
            {
                if (_SharedConn.State == ConnectionState.Open)
                {
                    await _SharedConn.CloseAsync();
                }
                await _SharedConn.DisposeAsync();
                _SharedConn = null;
            }

            // Step.3 清理 TransactionScope
            if (_CurrentScope != null)
            {
                _CurrentScope.Dispose(); // TransactionScope 目前沒有 DisposeAsync，只能用同步 Dispose
                _CurrentScope = null;
            }

            GC.SuppressFinalize(this);
        }
        # endregion
        # endregion

        # region Extensions
        # region private methods
        /// <summary>
        /// 將Model List轉為DataTable
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model List</param>
        /// <returns>DataTable</returns>
        private DataTable ModelToDataTable<T>(IEnumerable<T> data)
        {
            DataTable dt = new DataTable();
            IList<PropertyInfo> properties = GetModelMetadata(typeof(T)).AllProperties;

            dt.TableName = GetModelMetadata<T>().TableName;

            foreach (PropertyInfo prop in properties)
                dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (T item in data)
            {
                DataRow row = dt.NewRow();

                foreach (PropertyInfo prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;

                dt.Rows.Add(row);
            }

            return dt;
        }

        /// <summary>
        /// 將DataTable轉為Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="dt">DataTable</param>
        /// <returns>Model List</returns>
        private IList<T> DataTableToModel<T>(DataTable dt) where T : new()
        {
            // 建立回傳的 List
            List<T> list = new List<T>();

            if (dt == null || dt.Rows.Count == 0)
                return list;

            IList<PropertyInfo> properties = GetModelMetadata(typeof(T)).AllProperties;

            foreach (DataRow row in dt.Rows)
            {
                T item = new T();

                foreach (PropertyInfo prop in properties)
                {
                    // 名稱需完全一樣
                    if (dt.Columns.Contains(prop.Name))
                    {
                        object value = row[prop.Name];

                        // 排除 DBNull 且屬性必須可寫入 (CanWrite)
                        if (value != DBNull.Value && prop.CanWrite)
                        {
                            try
                            {
                                // 取得屬性的真實型別 (處理 int? 這種 Nullable 型別)
                                Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                                // 將資料庫的值轉換為屬性的型別 (防止 double 轉 decimal 等型別不符錯誤)
                                object safeValue;
                                if (targetType.IsEnum)
                                    safeValue = Enum.ToObject(targetType, value);       // 處理 Enum
                                else
                                    safeValue = Convert.ChangeType(value, targetType);  // 處理一般型別

                                prop.SetValue(item, safeValue);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        }
                    }
                }

                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// 產生單筆 Insert SQL
        /// </summary>
        /// <typeparam name="T">Data Model</typeparam>
        /// <returns>單筆新增SQL語法</returns>
        private string GenerateSingleInsertSQL<T>(){
            string tableName = GetModelMetadata<T>().TableName;

            IList<PropertyInfo> properties = GetModelMetadata<T>().InsertProperties;

            string columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
            string values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            // 加上 ; SELECT SCOPE_IDENTITY(); 以回傳剛剛新增的 ID
            // 加上 CAST 確保型別轉換順利，ex. 當 ExecuteScalar 在某些情況回傳 decimal 時
            string sql = $"INSERT INTO [{tableName}] ({columns}) VALUES ({values}); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return sql;
        }

        /// <summary>
        /// 共用連線是否開啟
        /// </summary>
        /// <returns>true:是/false:否</returns>
        private bool CheckSharedConnStatus(){
            return !(_SharedConn == null || _SharedConn.State != ConnectionState.Open);
        }

        /// <summary>
        /// 定義SqlBulkCopy設定
        /// </summary>
        /// <param name="bulk">SqlBulkCopy</param>
        /// <param name="dt">DataTable</param>
        /// <remarks>
        /// <para>建立Table Mapping</para>
        /// <para>建立欄位Mapping</para>
        /// <para>設定Timeout</para>
        /// </remarks>
        private void SetupBulkInsertSqlBulkCopy(SqlBulkCopy bulk, DataTable dt, int timeout = 600){
            bulk.DestinationTableName = dt.TableName;
            bulk.BulkCopyTimeout = timeout;

            // 依欄位名稱建立對應，確保資料不會錯位
            foreach (DataColumn col in dt.Columns)
                bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        /// <summary>
        /// 產生單筆 Update SQL
        /// </summary>
        /// <typeparam name="T">Data Model</typeparam>
        /// <returns>單筆更新SQL語法</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private string GenerateSingleUpdateSQL<T>()
        {
            string tableName = GetModelMetadata<T>().TableName;

            IList<PropertyInfo> setProperties = GetModelMetadata<T>().UpdateProperties;

            IList<PropertyInfo> keyProperties = GetModelMetadata<T>().KeyProperties;

            if (!setProperties.Any() || !keyProperties.Any())
                throw new InvalidOperationException("Update 操作需要至少一個可更新欄位以及至少一個 PrimaryKey 欄位。");

            string setClause = string.Join(", ", setProperties.Select(p => $"[{p.Name}] = @{p.Name}"));
            string whereClause = string.Join(" AND ", keyProperties.Select(p => $"[{p.Name}] = @{p.Name}"));

            string sql = $"UPDATE [{tableName}] SET {setClause} WHERE {whereClause}";

            return sql;
        }

        /// <summary>
        /// 產生批次更新臨時表名稱
        /// </summary>
        /// <typeparam name="T">Data Model</typeparam>
        /// <param name="dt">DataTable</param>
        /// <returns>TargetTableName:原始Table名稱/TempTableName:臨時表Table名稱</returns>
        private (string TargetTableName, string TempTableName) GenerateBulkUpdateTempTableName<T>(DataTable dt)
        {
            string targetTableName = dt.TableName;
            //SQL Server 的臨時表名稱長度有限制（最多 116 字元）
            string tempTableName = "#TempUpdate_" + Guid.NewGuid().ToString("N");
            return (targetTableName, tempTableName);
        }

        /// <summary>
        /// 產生批測更新 SQL
        /// </summary>
        /// <typeparam name="T">Data Model</typeparam>
        /// <param name="targetTableName">原始Table名稱</param>
        /// <param name="tempTableName">臨時表Table名稱</param>
        /// <returns>批次更新SQL語法</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private string GenerateBulkUpdateSQL<T>(string targetTableName, string tempTableName)
        {
            IList<PropertyInfo> setProperties = GetModelMetadata<T>().UpdateProperties;
            IList<PropertyInfo> keyProperties = GetModelMetadata<T>().KeyProperties;

            if (!setProperties.Any() || !keyProperties.Any())
                throw new InvalidOperationException("Update 操作需要至少一個可更新欄位以及至少一個 PrimaryKey 欄位。");

            string setClause = string.Join(", ", setProperties.Select(p => $"T.{p.Name} = Temp.{p.Name}"));
            string onClause = string.Join(" AND ", keyProperties.Select(p => $"T.{p.Name} = Temp.{p.Name}"));

            string sql = $@"
                    UPDATE T
                    SET {setClause}
                    FROM [{targetTableName}] T
                    JOIN [{tempTableName}] Temp ON {onClause};
                ";

            return sql;
        }

        /// <summary>
        /// 產生 Delete SQL
        /// </summary>
        /// <typeparam name="T">Data Model</typeparam>
        /// <returns>刪除SQL語法</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private string GenerateDeleteSQL<T>()
        {
            string tableName = GetModelMetadata<T>().TableName;

            IList<PropertyInfo> keyProperties = GetModelMetadata<T>().KeyProperties;

            if (!keyProperties.Any())
                throw new InvalidOperationException("Delete 操作需要至少一個 PrimaryKey 欄位。");

            string whereClause = string.Join(" AND ", keyProperties.Select(p => $"{p.Name} = @{p.Name}"));
            string sql = $"DELETE FROM [{tableName}] WHERE {whereClause}";

            return sql;
        }
        # endregion

        # region public methods
        public IList<T> ExecuteSQL<T>(string sql, object? parameters = null) where T : new()
        {
            DataTable dt = ExecuteSQL(sql, parameters);

            return DataTableToModel<T>(dt);
        }

        public async Task<IList<T>> ExecuteSQLAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default) where T : new()
        {
            DataTable dt = await ExecuteSQLAsync(sql, parameters, cancellationToken);
            return DataTableToModel<T>(dt);
        }

        public int SingleInsert<T>(T data)
        {
            string sql = GenerateSingleInsertSQL<T>();

            // 用 ExecuteScalar 取得 SELECT SCOPE_IDENTITY() 的結果
            object? result = ExecuteScalar(sql, data);

            // 轉型並回傳 ID
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<int> SingleInsertAsync<T>(T data, CancellationToken cancellationToken = default)
        {
            string sql = GenerateSingleInsertSQL<T>();

            // 用 ExecuteScalar 取得 SELECT SCOPE_IDENTITY() 的結果
            object? result = await ExecuteScalarAsync(sql, data, cancellationToken);

            // 轉型並回傳 ID
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public void BulkInsert<T>(IEnumerable<T> data)
        {
            DataTable dt = ModelToDataTable(data);

            BulkInsert(dt);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default)
        {
            DataTable dt = ModelToDataTable(data);

            await BulkInsertAsync(dt, cancellationToken);
        }

        public void BulkInsert(DataTable dt)
        {
            // 判斷是否使用共用連線
            // 如果有共用連線 (_SharedConn)，就必須使用它，並帶上交易 (_SharedTrans)
            // 外部需呼叫 OpenSharedConnection

            // 確保連線是開的
            if (!CheckSharedConnStatus())
                throw new InvalidOperationException("使用 BulkInsert 前請確保連線已開啟");

            using (SqlBulkCopy bulk = new SqlBulkCopy(_SharedConn, SqlBulkCopyOptions.Default, _SharedTrans))
            {
                SetupBulkInsertSqlBulkCopy(bulk, dt);

                bulk.WriteToServer(dt);
            }
        }

        public async Task BulkInsertAsync(DataTable dt, CancellationToken cancellationToken = default)
        {
            // 判斷是否使用共用連線
            // 如果有共用連線 (_SharedConn)，就必須使用它，並帶上交易 (_SharedTrans)
            // 外部需呼叫 OpenSharedConnection

            // 確保連線是開的
            if (!CheckSharedConnStatus())
                throw new InvalidOperationException("使用 BulkInsert 前請確保連線已開啟");

            using (SqlBulkCopy bulk = new SqlBulkCopy(_SharedConn, SqlBulkCopyOptions.Default, _SharedTrans))
            {
                SetupBulkInsertSqlBulkCopy(bulk, dt);

                await bulk.WriteToServerAsync(dt, cancellationToken);
            }
        }

        public int SingleUpdate<T>(T data)
        {
            string sql = GenerateSingleUpdateSQL<T>();

            return ExecuteNonQuery(sql, data);
        }

        public async Task<int> SingleUpdateAsync<T>(T data, CancellationToken cancellationToken = default)
        {
            string sql = GenerateSingleUpdateSQL<T>();

            return await ExecuteNonQueryAsync(sql, data, cancellationToken);
        }

        public void BulkUpdate<T>(IEnumerable<T> data)
        {
            // 標記這次操作是否是由此方法「主動」開啟連線的
            bool isLocalOpen = false, isLocalTransaction = false;

            // 如果使用者沒有先開啟共用連線，ExecuteNonQuery會關閉連線，且建立的Temp也會不見
            // 檢查連線狀態：如果是關閉的，就強制打開，並標記起來
            if (_SharedConn == null || _SharedConn.State != ConnectionState.Open)
            {
                OpenSharedConnection();
                isLocalOpen = true;
            }

            // 如果當前沒有 TransactionScope 也沒有 SharedTrans，就開啟一個本地交易來保護整個 BulkUpdate 過程
            if (_SharedTrans == null && Transaction.Current == null)
            {
                BeginTransaction();
                isLocalTransaction = true;
            }

            DataTable dt = ModelToDataTable(data);

            (string targetTableName, string tempTableName) = GenerateBulkUpdateTempTableName<T>(dt);

            try
            {
                ExecuteNonQuery($"SELECT * INTO [{tempTableName}] FROM [{targetTableName}] WHERE 1 = 0");

                dt.TableName = tempTableName;
                BulkInsert(dt);

                string sql = GenerateBulkUpdateSQL<T>(targetTableName, tempTableName);

                ExecuteNonQuery(sql);

                // 如果是本地開啟的交易，執行成功後 Commit
                if (isLocalTransaction) Commit();
            }
            catch
            {
                // 發生錯誤，如果是本地交易則 Rollback
                if (isLocalTransaction) Rollback();
                throw;
            }
            finally
            {
                try { ExecuteNonQuery($"DROP TABLE IF EXISTS [{tempTableName}]"); }
                catch(Exception ex) { Debug.WriteLine(ex.Message); }

                // 如果原本外面就有開啟交易 (Transaction)，這裡就不能關，否則會中斷交易
                if (isLocalOpen) _SharedConn?.Close();
            }
        }

        public async Task BulkUpdateAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default)
        {
            // 標記這次操作是否是由此方法「主動」開啟連線的
            bool isLocalOpen = false, isLocalTransaction = false;

            // 如果使用者沒有先開啟共用連線，ExecuteNonQuery會關閉連線，且建立的Temp也會不見
            // 檢查連線狀態：如果是關閉的，就強制打開，並標記起來
            if (_SharedConn == null || _SharedConn.State != ConnectionState.Open)
            {
                await OpenSharedConnectionAsync(cancellationToken);
                isLocalOpen = true;
            }

            // 如果當前沒有 TransactionScope 也沒有 SharedTrans，就開啟一個本地交易來保護整個 BulkUpdate 過程
            if (_SharedTrans == null && Transaction.Current == null)
            {
                await BeginTransactionAsync(cancellationToken);
                isLocalTransaction = true;
            }

            DataTable dt = ModelToDataTable(data);

            (string targetTableName, string tempTableName) = GenerateBulkUpdateTempTableName<T>(dt);

            try
            {
                await ExecuteNonQueryAsync($"SELECT * INTO [{tempTableName}] FROM [{targetTableName}] WHERE 1 = 0", null, cancellationToken);

                dt.TableName = tempTableName;
                await BulkInsertAsync(dt, cancellationToken);

                string sql = GenerateBulkUpdateSQL<T>(targetTableName, tempTableName);

                await ExecuteNonQueryAsync(sql, null, cancellationToken);

                // 如果是本地開啟的交易，執行成功後 Commit
                if (isLocalTransaction) await CommitAsync(cancellationToken);
            }
            catch
            {
                // 發生錯誤，如果是本地交易則 Rollback
                if (isLocalTransaction) await RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                try { await ExecuteNonQueryAsync($"DROP TABLE IF EXISTS [{tempTableName}]", cancellationToken); }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }

                // 如果原本外面就有開啟交易 (Transaction)，這裡就不能關，否則會中斷交易
                if (isLocalOpen && _SharedConn != null)
                {
                    await _SharedConn.CloseAsync();
                }
            }
        }

        public int Delete<T>(T data)
        {
            string sql = GenerateDeleteSQL<T>();

            return ExecuteNonQuery(sql, data);
        }

        public async Task<int> DeleteAsync<T>(T data, CancellationToken cancellationToken = default)
        {
            string sql = GenerateDeleteSQL<T>();

            return await ExecuteNonQueryAsync(sql, data, cancellationToken);
        }

        public DataTable ExecuteStoredProcedure(string spName, object? parameters = null)
        {
            SqlConnection conn = GetConnection();
            bool isInternalConn = _SharedConn == null;

            try
            {
                // 指定 CommandType.StoredProcedure
                using (SqlCommand cmd = CreateCommand(spName, conn, CommandType.StoredProcedure))
                {
                    AddParameters(cmd, parameters);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public async Task<DataTable> ExecuteStoredProcedureAsync(string spName, object? parameters = null,  CancellationToken cancellationToken = default)
        {
            SqlConnection conn = await GetConnectionAsync(cancellationToken);
            bool isInternalConn = _SharedConn == null;
            try
            {
                // 指定 CommandType.StoredProcedure
                using (SqlCommand cmd = CreateCommand(spName, conn,  CommandType.StoredProcedure))
                {
                    AddParameters(cmd, parameters);
                    // 使用 ExecuteReaderAsync
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        DataTable dt = new DataTable();
                        // 這裡使用 dt.Load(reader) 
                        // 但 dt.Load 本身是同步的。如果要純非同步，需手動寫迴圈讀取
                        // 實務上在 .NET 8，為了方便通常接受 dt.Load(reader) 的短暫 CPU 阻塞，
                        // 或者寫一個 Helper 使用 await reader.ReadAsync() 來填充
                        dt.Load(reader);
                        return dt;
                    }
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public IList<T> ExecuteStoredProcedure<T>(string spName, object? parameters = null) where T : new()
        {
            DataTable dt = ExecuteStoredProcedure(spName, parameters);
            return DataTableToModel<T>(dt);
        }

        public async Task<IList<T>> ExecuteStoredProcedureAsync<T>(string spName, object? parameters = null, CancellationToken cancellationToken = default) where T : new()
        {
            DataTable dt = await ExecuteStoredProcedureAsync(spName, parameters, cancellationToken);
            return DataTableToModel<T>(dt);
        }

        public int ExecuteStoredProcedureNonQuery(string spName, object? parameters = null)
        {
            SqlConnection conn = GetConnection();
            bool isInternalConn = _SharedConn == null;

            try
            {
                using (SqlCommand cmd = CreateCommand(spName, conn, CommandType.StoredProcedure))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }

        public async Task<int> ExecuteStoredProcedureNonQueryAsync(string spName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            SqlConnection conn = await GetConnectionAsync(cancellationToken);
            bool isInternalConn = _SharedConn == null;
            try
            {
                using (SqlCommand cmd = CreateCommand(spName, conn, CommandType.StoredProcedure))
                {
                    AddParameters(cmd, parameters);
                    return await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                if (isInternalConn) CloseInternalConnection(conn);
            }
        }
        # endregion
        # endregion
    }
}