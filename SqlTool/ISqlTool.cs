using System.Data;

namespace SeanTool.CSharp.Net8
{
    public interface ISqlTool
    {
        void BeginTransaction();
        void Commit();
        void CommitScope();
        void Dispose();
        int ExecuteNonQuery(string sql, object? parameters = null);
        object? ExecuteScalar(string sql, object? parameters = null);
        DataTable GetDataTable(string sql, object? parameters = null);
        void OpenSharedConnection();
        void Rollback();
        void StartTransactionScope(System.Transactions.IsolationLevel level = System.Transactions.IsolationLevel.ReadCommitted, int timeoutSeconds = 60);
    }
}