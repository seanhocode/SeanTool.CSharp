using System.Data;

namespace SeanTool.CSharp
{
    public interface ISqlTool : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 開啟共用連線
        /// </summary>
        /// <remarks>在整個 Scope 裡面只開一次連線，並一直重複使用它，直到 Scope 結束</remarks>
        void OpenSharedConnection();

        /// <summary>
        /// 非同步開啟共用連線
        /// </summary>
        /// <remarks>在整個 Scope 裡面只開一次連線，並一直重複使用它，直到 Scope 結束</remarks>
        Task OpenSharedConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 開啟交易
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 非同步開啟交易
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 提交交易
        /// </summary>
        void Commit();

        /// <summary>
        /// 非同步提交交易
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消交易
        /// </summary>
        void Rollback();

        /// <summary>
        /// 非同步取消交易
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 開啟 TransactionScope 模式
        /// </summary>
        /// <param name="level">隔離層級 (預設 ReadCommitted)</param>
        /// <param name="timeoutSeconds">超時秒數 (預設 60秒)</param>
        /// <remarks>開啟此模式後會開啟共用連線，交易結束後需呼叫CommitScope()提交交易</remarks>
        void StartTransactionScope(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.ReadCommitted, int timeoutSeconds = 60);

        /// <summary>
        /// 提交 TransactionScope
        /// </summary>
        void CommitScope();

        /// <summary>
        /// 執行增刪改
        /// </summary>
        /// <param name="sql">SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>執行筆數</returns>
        int ExecuteNonQuery(string sql, object? parameters = null);

        /// <summary>
        /// 非同步執行增刪改
        /// </summary>
        /// <param name="sql">SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>執行筆數</returns>
        Task<int> ExecuteNonQueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查詢單一值
        /// </summary>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果object</returns>
        object? ExecuteScalar(string sql, object? parameters = null);

        /// <summary>
        /// 非同步查詢單一值
        /// </summary>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果object</returns>
        Task<object?> ExecuteScalarAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查詢資料表
        /// </summary>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果DataTable</returns>
        DataTable ExecuteSQL(string sql, object? parameters = null);

        /// <summary>
        /// 非同步查詢資料表
        /// </summary>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果DataTable</returns>
        Task<DataTable> ExecuteSQLAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查詢資料並轉為Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果Model List</returns>
        IList<T> ExecuteSQL<T>(string sql, object? parameters = null) where T : new();

        /// <summary>
        /// 非同步查詢資料並轉為Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果Model List</returns>
        Task<IList<T>> ExecuteSQLAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default) where T : new();

        /// <summary>
        /// 單一Insert資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns>自動增量的主鍵 (Identity ID)</returns>
        int SingleInsert<T>(T data);

        /// <summary>
        /// 非同步單一Insert資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns>自動增量的主鍵 (Identity ID)</returns>
        Task<int> SingleInsertAsync<T>(T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 整批Insert資料 by Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model List</param>
        void BulkInsert<T>(IEnumerable<T> data);

        /// <summary>
        /// 非同步整批Insert資料 by Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model List</param>
        Task BulkInsertAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 整批Insert資料 by DataTable
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <exception cref="InvalidOperationException">連線未開啟，請使用OpenSharedConnection開啟連線</exception>
        void BulkInsert(DataTable dt);

        /// <summary>
        /// 非同步整批Insert資料 by DataTable
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <exception cref="InvalidOperationException">連線未開啟，請使用OpenSharedConnection開啟連線</exception>
        Task BulkInsertAsync(DataTable dt, CancellationToken cancellationToken = default);

        /// <summary>
        /// 單一Update資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns>執行筆數</returns>
        /// <exception cref="InvalidOperationException">無法對應或無可更新欄位</exception>
        int SingleUpdate<T>(T data);

        /// <summary>
        /// 非同步單一Update資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns>執行筆數</returns>
        /// <exception cref="InvalidOperationException">無法對應或無可更新欄位</exception>
        Task<int> SingleUpdateAsync<T>(T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 整批Update資料 by Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <exception cref="InvalidOperationException">無法對應或無可更新欄位</exception>
        void BulkUpdate<T>(IEnumerable<T> data);

        /// <summary>
        /// 非同步整批Update資料 by Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <exception cref="InvalidOperationException">無法對應或無可更新欄位</exception>
        Task BulkUpdateAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">無法對應欄位</exception>
        int Delete<T>(T data);

        /// <summary>
        /// 非同步Delete資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">無法對應欄位</exception>
        Task<int> DeleteAsync<T>(T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 執行 Stored Procedure 並回傳 DataTable
        /// </summary>
        /// <param name="spName">Stored Procedure 名稱</param>
        /// <param name="parameters">參數，可為 IDictionary、Object 或 IEnumerable&lt;SqlParameter&gt;</param>
        /// <returns>查詢結果 DataTable</returns>
        DataTable ExecuteStoredProcedure(string spName, object? parameters = null);

        /// <summary>
        /// 非同步執行 Stored Procedure 並回傳 DataTable
        /// </summary>
        /// <param name="spName">Stored Procedure 名稱</param>
        /// <param name="parameters">參數，可為 IDictionary、Object 或 IEnumerable&lt;SqlParameter&gt;</param>
        /// <returns>查詢結果 DataTable</returns>
        Task<DataTable> ExecuteStoredProcedureAsync(string spName, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 執行 Stored Procedure 並回傳 Model List
        /// </summary>
        /// <typeparam name="T">Model 型別</typeparam>
        /// <param name="spName">Stored Procedure 名稱</param>
        /// <param name="parameters">參數，可為 IDictionary、Object 或 IEnumerable&lt;SqlParameter&gt;</param>
        /// <returns>查詢結果 Model List</returns>
        IList<T> ExecuteStoredProcedure<T>(string spName, object? parameters = null) where T : new();

        /// <summary>
        /// 非同步執行 Stored Procedure 並回傳 Model List
        /// </summary>
        /// <typeparam name="T">Model 型別</typeparam>
        /// <param name="spName">Stored Procedure 名稱</param>
        /// <param name="parameters">參數，可為 IDictionary、Object 或 IEnumerable&lt;SqlParameter&gt;</param>
        /// <returns>查詢結果 Model List</returns>
        Task<IList<T>> ExecuteStoredProcedureAsync<T>(string spName, object? parameters = null, CancellationToken cancellationToken = default) where T : new();

        /// <summary>
        /// 執行 Stored Procedure (不回傳資料)
        /// </summary>
        /// <param name="spName">Stored Procedure 名稱</param>
        /// <param name="parameters">參數，可為 IDictionary、Object 或 IEnumerable&lt;SqlParameter&gt;</param>
        /// <returns>執行影響筆數</returns>
        int ExecuteStoredProcedureNonQuery(string spName, object? parameters = null);

        /// <summary>
        /// 非同步執行 Stored Procedure (不回傳資料)
        /// </summary>
        /// <param name="spName">Stored Procedure 名稱</param>
        /// <param name="parameters">參數，可為 IDictionary、Object 或 IEnumerable&lt;SqlParameter&gt;</param>
        /// <returns>執行影響筆數</returns>
        Task<int> ExecuteStoredProcedureNonQueryAsync(string spName, object? parameters = null, CancellationToken cancellationToken = default);
    }
}