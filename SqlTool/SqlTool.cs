using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Transactions;

namespace SeanTool.CSharp.Net8
{
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
            // 把剛剛那段註冊邏輯藏在這裡
            services.AddSingleton<ISqlTool>(sp => new SqlTool(connectionString));
            return services;
        }
    }

    public class SqlTool : IDisposable, ISqlTool
    {
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
                    if (!string.IsNullOrEmpty(key))
                        cmd.Parameters.AddWithValue(key, entry.Value ?? DBNull.Value);
                }
            }
            else
            {
                foreach (PropertyInfo prop in parameters.GetType().GetProperties())
                {
                    cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters) ?? DBNull.Value);
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
    }
}
