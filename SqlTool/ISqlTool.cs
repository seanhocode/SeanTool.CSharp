using System.Data;

namespace SeanTool.CSharp.Net8
{
    public interface ISqlTool : IDisposable
    {
        /// <summary>
        /// 開啟共用連線
        /// </summary>
        /// <remarks>在整個 Scope 裡面只開一次連線，並一直重複使用它，直到 Scope 結束</remarks>
        void OpenSharedConnection();

        /// <summary>
        /// 開啟交易
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 提交交易
        /// </summary>
        void Commit();

        /// <summary>
        /// 取消交易
        /// </summary>
        void Rollback();

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
        /// 查詢單一值
        /// </summary>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果object</returns>
        object? ExecuteScalar(string sql, object? parameters = null);

        /// <summary>
        /// 查詢資料表
        /// </summary>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果DataTable</returns>
        DataTable ExecuteSQL(string sql, object? parameters = null);

        /// <summary>
        /// 查詢資料並轉為Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="sql">查詢SQL語法</param>
        /// <param name="parameters">SQL參數，可為IDictionary、ObjectList</param>
        /// <returns>查詢結果Model List</returns>
        IList<T> ExecuteSQL<T>(string sql, object? parameters = null) where T : new();

        /// <summary>
        /// 單一Insert資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns>執行筆數</returns>
        int SingleInsert<T>(T data);

        /// <summary>
        /// 整批Insert資料 by Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model List</param>
        void BulkInsert<T>(IEnumerable<T> data);

        /// <summary>
        /// 整批Insert資料 by DataTable
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <exception cref="InvalidOperationException">連線未開啟，請使用OpenSharedConnection開啟連線</exception>
        void BulkInsert(DataTable dt);

        /// <summary>
        /// 單一Update資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns>執行筆數</returns>
        /// <exception cref="InvalidOperationException">無法對應或無可更新欄位</exception>
        int SingleUpdate<T>(T data);

        /// <summary>
        /// 整批Update資料 by Model List
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <exception cref="InvalidOperationException">無法對應或無可更新欄位</exception>
        void BulkUpdate<T>(IEnumerable<T> data);

        /// <summary>
        /// Delete資料 by Model
        /// </summary>
        /// <typeparam name="T">Model</typeparam>
        /// <param name="data">Model Data</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">無法對應欄位</exception>
        int Delete<T>(T data);
    }
}