using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Transactions;

namespace SeanTool.CSharp.Net8
{
    public class TableNameAttribute : Attribute
    {
        public string TableName { get; }

        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]// 限制只能貼在屬性上
    public class PrimaryKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]// 限制只能貼在屬性上
    public class IdentityAttribute : Attribute
    {
    }

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

    public class SqlTool : IDisposable, ISqlTool
    {
        # region Main
        private readonly string _ConnectionString;

        // 暫存共用連線與交易
        private SqlConnection? _SharedConn;
        private SqlTransaction? _SharedTrans;

        // 暫存 TransactionScope
        private TransactionScope? _CurrentScope;

        public SqlTool(string connStr)
        {
            _ConnectionString = connStr;
        }

        /// <summary>
        /// 開啟共用連線模式 (Scoped Start)
        /// </summary>
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

        # region TransactionScope
        /// <summary>
        /// 開啟 TransactionScope 模式
        /// </summary>
        /// <param name="level">隔離層級 (預設 ReadCommitted)</param>
        /// <param name="timeoutSeconds">超時秒數 (預設 60秒)</param>
        public void StartTransactionScope(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.ReadCommitted, int timeoutSeconds = 60)
        {
            if (_SharedTrans != null)
            {
                throw new InvalidOperationException("已在手動 SqlTransaction 模式中，不可混合使用 TransactionScope。");
            }

            if (_SharedConn != null && _SharedConn.State == ConnectionState.Open)
            {
                throw new InvalidOperationException("連線已開啟，無法建立 TransactionScope。請確保在開啟 Scope 前連線是關閉的，或讓 Scope 自動管理連線。");
            }

            // Step.1 設定 TransactionOptions
            TransactionOptions options = new TransactionOptions
            {
                IsolationLevel = level,
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            // Step.2 建立 Scope
            // TransactionScopeAsyncFlowOption.Enabled 是為了支援 async/await (雖然目前你用同步，但保留彈性較佳)
            _CurrentScope = new TransactionScope(TransactionScopeOption.Required, options, TransactionScopeAsyncFlowOption.Enabled);

            // Step.3 關鍵：Scope 建立後，立刻開啟共用連線
            // 這樣這條連線就會自動加入這個 Scope，避免自動開啟多條連線導致 MSDTC
            OpenSharedConnection();
        }

        /// <summary>
        /// 提交 TransactionScope
        /// </summary>
        public void CommitScope()
        {
            _CurrentScope?.Complete();
        }
        #endregion

        #region SQLTransaction
        /// <summary>
        /// 開啟交易
        /// </summary>
        public void BeginTransaction()
        {
            if (_CurrentScope != null)
            {
                throw new InvalidOperationException("已在 TransactionScope 模式中，不可混合使用手動 SqlTransaction。");
            }

            OpenSharedConnection();
            if (_SharedTrans == null)
            {
                _SharedTrans = _SharedConn!.BeginTransaction();
            }
        }

        /// <summary>
        /// 提交交易
        /// </summary>
        public void Commit()
        {
            _SharedTrans?.Commit();
            _SharedTrans = null; // 清空交易物件，準備下一次
        }

        /// <summary>
        /// 回滾交易
        /// </summary>
        public void Rollback()
        {
            _SharedTrans?.Rollback();
            _SharedTrans = null;
        }
        #endregion

        private SqlConnection GetConnection()
        {
            SqlConnection conn = _SharedConn ?? new SqlConnection(_ConnectionString);
            if (conn.State != ConnectionState.Open) conn.Open();
            return conn;
        }

        private SqlCommand CreateCommand(string sql, SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand(sql, conn);
            // 只有在手動交易模式下才需要指定 Transaction
            // TransactionScope 模式下，ADO.NET 會自動處理，不需要指定 cmd.Transaction
            if (_SharedTrans != null)
            {
                cmd.Transaction = _SharedTrans;
            }
            return cmd;
        }

        private void CloseInternalConnection(SqlConnection conn)
        {
            conn.Close();
            conn.Dispose();
        }

        private (SqlDbType Type, int Size) GetSqlDbType(Type type)
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
            if (underlyingType == typeof(string)) return (SqlDbType.VarChar, -1);

            // 預設 fallback
            return (SqlDbType.Variant, 0);
        }

        /// <summary>
        /// 處理參數
        /// </summary>
        private void AddParameters(SqlCommand cmd, object? parameters)
        {
            if (parameters == null) return;

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

                    var (dbType, size) = GetSqlDbType(valType);

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
                foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                {
                    // 1. 取得對應的 SqlDbType
                    var (dbType, size) = GetSqlDbType(prop.PropertyType);

                    // 2. 建立參數 (明確指定型別，解決隱性轉型效能問題)
                    SqlParameter param = cmd.Parameters.Add($"@{prop.Name}", dbType, size);

                    // 3. 設定值
                    param.Value = prop.GetValue(parameters) ?? DBNull.Value;
                }
            }
        }

        /// <summary>
        /// 執行增刪改
        /// </summary>
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

        /// <summary>
        /// 查詢單一值
        /// </summary>
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

        /// <summary>
        /// 查詢資料表
        /// </summary>
        public DataTable GetDataTable(string sql, object? parameters = null)
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

        /// <summary>
        /// 實作 IDisposable，確保共用連線最後會被關閉
        /// </summary>
        public void Dispose()
        {
            // 1. 清理 SqlTransaction
            if (_SharedTrans != null)
            {
                _SharedTrans.Dispose();
                _SharedTrans = null;
            }

            // 2. 清理連線 (通常建議先關連線，再關 Scope，或者讓 Scope 自動退回)
            // 為了安全，我們先明確關閉連線
            if (_SharedConn != null)
            {
                if (_SharedConn.State == ConnectionState.Open)
                    _SharedConn.Close();
                _SharedConn.Dispose();
                _SharedConn = null;
            }

            // 3. 清理 TransactionScope
            if (_CurrentScope != null)
            {
                _CurrentScope.Dispose();
                _CurrentScope = null;
            }

            GC.SuppressFinalize(this);
        }
        # endregion

        # region Extensions
        private string GetTableName<T>()
        {
            Type type = typeof(T);
            TableNameAttribute? attr = type.GetCustomAttributes(typeof(TableNameAttribute), true)
                                                .FirstOrDefault() as TableNameAttribute;

            if (attr != null)
                return attr.TableName;

            return type.Name;
        }

        private IList<PropertyInfo> GetInsertProperties<T>()
        {
            return typeof(T).GetProperties().Where(p => p.GetCustomAttribute<IdentityAttribute>() == null).ToList();
        }

        private IList<PropertyInfo> GetSetProperties<T>()
        {
            return typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>() == null
                         && p.GetCustomAttribute<IdentityAttribute>() == null)
                .ToList();
        }

        private IList<PropertyInfo> GetKeyProperties<T>()
        {
            return typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                .ToList();
        }

        private DataTable ModelToDataTable<T>(IEnumerable<T> data)
        {
            DataTable dt = new DataTable();
            PropertyInfo[] properties = typeof(T).GetProperties();

            dt.TableName = GetTableName<T>();

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

        public int SingleInsert<T>(T data)
        {
            string tableName = GetTableName<T>();

            IList<PropertyInfo> properties = GetInsertProperties<T>();

            string columns = string.Join(", ", properties.Select(p => p.Name));
            string values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            return ExecuteNonQuery(sql, data);
        }

        public void BulkInsert<T>(IEnumerable<T> data)
        {
            DataTable dt = ModelToDataTable(data);

            BulkInsert(dt);
        }

        public void BulkInsert(DataTable dt)
        {
            // 判斷是否使用共用連線
            // 如果有共用連線 (_SharedConn)，就必須使用它，並帶上交易 (_SharedTrans)
            // 外部需呼叫 OpenSharedConnection

            // 確保連線是開的
            if (_SharedConn == null || _SharedConn.State != ConnectionState.Open)
                throw new InvalidOperationException("使用 BulkInsert 前請確保連線已開啟 (OpenSharedConnection)");

            using (SqlBulkCopy bulk = new SqlBulkCopy(_SharedConn, SqlBulkCopyOptions.Default, _SharedTrans))
            {
                bulk.DestinationTableName = dt.TableName;
                bulk.BulkCopyTimeout = 600;

                // 依欄位名稱建立對應，確保資料不會錯位
                foreach (DataColumn col in dt.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

                bulk.WriteToServer(dt);
            }
        }

        public int SingleUpdate<T>(T data)
        {
            string tableName = GetTableName<T>();

            IList<PropertyInfo> setProperties = GetSetProperties<T>();

            IList<PropertyInfo> keyProperties = GetKeyProperties<T>();

            if (!setProperties.Any() || !keyProperties.Any())
                throw new InvalidOperationException("Update 操作需要至少一個可更新欄位以及至少一個 PrimaryKey 欄位。");

            string setClause = string.Join(", ", setProperties.Select(p => $"{p.Name} = @{p.Name}"));
            string whereClause = string.Join(" AND ", keyProperties.Select(p => $"{p.Name} = @{p.Name}"));

            string sql = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";

            return ExecuteNonQuery(sql, data);
        }

        public void BulkUpdate<T>(IEnumerable<T> data)
        {
            // 標記這次操作是否是由此方法「主動」開啟連線的
            bool isLocalOpen = false;

            // 如果使用者沒有先開啟共用連線，ExecuteNonQuery會關閉連線，且建立的Temp也會不見
            // 檢查連線狀態：如果是關閉的，就強制打開，並標記起來
            if (_SharedConn == null || _SharedConn.State != ConnectionState.Open)
            {
                OpenSharedConnection();
                isLocalOpen = true;
            }

            DataTable dt = ModelToDataTable(data);
            string targetTableName = dt.TableName;
            string tempTableName = "#TempUpdate_" + Guid.NewGuid().ToString("N");

            try
            {
                ExecuteNonQuery($"SELECT * INTO {tempTableName} FROM {targetTableName} WHERE 1 = 0");

                dt.TableName = tempTableName;
                BulkInsert(dt);

                IList<PropertyInfo> setProperties = GetSetProperties<T>();
                IList<PropertyInfo> keyProperties = GetKeyProperties<T>();

                string setClause = string.Join(", ", setProperties.Select(p => $"T.{p.Name} = Temp.{p.Name}"));
                string onClause = string.Join(" AND ", keyProperties.Select(p => $"T.{p.Name} = Temp.{p.Name}"));

                string sql = $@"
                    UPDATE T
                    SET {setClause}
                    FROM {targetTableName} T
                    JOIN {tempTableName} Temp ON {onClause};
                ";

                ExecuteNonQuery(sql);
            }
            finally
            {
                ExecuteNonQuery($"DROP TABLE IF EXISTS {tempTableName}");

                // 如果原本外面就有開啟交易 (Transaction)，這裡就不能關，否則會中斷交易
                if (isLocalOpen)
                {
                    _SharedConn?.Close();
                }
            }
        }

        public int Delete<T>(T data)
        {
            string tableName = GetTableName<T>();

            var keyProperties = GetKeyProperties<T>();

            if (!keyProperties.Any())
                throw new InvalidOperationException("Delete 操作需要至少一個 PrimaryKey 欄位。");

            string whereClause = string.Join(" AND ", keyProperties.Select(p => $"{p.Name} = @{p.Name}"));
            string sql = $"DELETE FROM {tableName} WHERE {whereClause}";

            return ExecuteNonQuery(sql, data);
        }
        # endregion
    }
}
