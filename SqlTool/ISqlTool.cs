using System.Data;

namespace SeanTool.CSharp.Net8
{
    public interface ISqlTool : IDisposable
    {
        void BeginTransaction();
        void BulkInsert(DataTable dt);
        void BulkInsert<T>(IEnumerable<T> data);
        void BulkUpdate<T>(IEnumerable<T> data);
        void Commit();
        void CommitScope();
        int Delete<T>(T data);
        void Dispose();
        int ExecuteNonQuery(string sql, object? parameters = null);
        object? ExecuteScalar(string sql, object? parameters = null);
        DataTable GetDataTable(string sql, object? parameters = null);
        void OpenSharedConnection();
        void Rollback();
        int SingleInsert<T>(T data);
        int SingleUpdate<T>(T data);
        void StartTransactionScope(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.ReadCommitted, int timeoutSeconds = 60);
    }
}