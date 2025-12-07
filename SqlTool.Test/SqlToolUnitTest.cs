using System.Data;

namespace SeanTool.CSharp.Net8.Test
{
    public class SqlToolUnitTest : IDisposable
    {
        // 使用 LocalDB 進行測試，通常 Visual Studio 安裝後都會有
        private const string _ConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=tempdb;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly string _TableName;

        public SqlToolUnitTest()
        {
            // 每次測試都建立一個隨機名稱的 Table，避免並行測試衝突
            _TableName = $"TestTable_{Guid.NewGuid().ToString("N")}";
            CreateTestTable();
        }

        private void CreateTestTable()
        {
            using var tool = new SqlTool(_ConnStr);
            string sql = $@"CREATE TABLE {_TableName} (
                            Id INT PRIMARY KEY IDENTITY, 
                            Name NVARCHAR(50), 
                            Age INT
                          )";
            tool.ExecuteNonQuery(sql);
        }

        public void Dispose()
        {
            // 清理測試 Table
            try
            {
                using var tool = new SqlTool(_ConnStr);
                tool.ExecuteNonQuery($"DROP TABLE IF EXISTS {_TableName}");
            }
            catch { /* 忽略清理錯誤 */ }
        }

        [Fact]
        public void ExecuteScalar_SelectOne_ReturnsOne()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);

            // Act
            var result = tool.ExecuteScalar("SELECT 1");

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void ExecuteNonQuery_InsertWithAnonymousObject_InsertsData()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);
            var param = new { Name = "Sean", Age = 30 };
            string sql = $"INSERT INTO {_TableName} (Name, Age) VALUES (@Name, @Age)";

            // Act
            int affected = tool.ExecuteNonQuery(sql, param);

            // Assert
            Assert.Equal(1, affected);

            var count = tool.ExecuteScalar($"SELECT COUNT(*) FROM {_TableName}");
            Assert.Equal(1, count);
        }

        [Fact]
        public void ExecuteNonQuery_InsertWithDictionary_InsertsData()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);
            var param = new Dictionary<string, object>
            {
                { "@Name", "TestUser" },
                { "@Age", 99 }
            };
            string sql = $"INSERT INTO {_TableName} (Name, Age) VALUES (@Name, @Age)";

            // Act
            tool.ExecuteNonQuery(sql, param);

            // Assert
            var name = tool.ExecuteScalar($"SELECT Name FROM {_TableName}");
            Assert.Equal("TestUser", name);
        }

        [Fact]
        public void GetDataTable_ReturnsCorrectData()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);
            tool.ExecuteNonQuery($"INSERT INTO {_TableName} (Name) VALUES ('Row1'), ('Row2')");

            // Act
            DataTable dt = tool.GetDataTable($"SELECT * FROM {_TableName}");

            // Assert
            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal("Row1", dt.Rows[0]["Name"]);
        }

        #region Transaction Tests (Manual)

        [Fact]
        public void BeginTransaction_Rollback_DataNotSaved()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);

            // Act
            tool.BeginTransaction();
            tool.ExecuteNonQuery($"INSERT INTO {_TableName} (Name) VALUES ('RollbackTarget')");
            tool.Rollback();

            // Assert
            var count = tool.ExecuteScalar($"SELECT COUNT(*) FROM {_TableName}");
            Assert.Equal(0, count); // 資料應該被清空
        }

        [Fact]
        public void BeginTransaction_Commit_DataSaved()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);

            // Act
            tool.BeginTransaction();
            tool.ExecuteNonQuery($"INSERT INTO {_TableName} (Name) VALUES ('CommitTarget')");
            tool.Commit();

            // Assert
            var count = tool.ExecuteScalar($"SELECT COUNT(*) FROM {_TableName}");
            Assert.Equal(1, count);
        }

        #endregion

        #region TransactionScope Tests

        [Fact]
        public void TransactionScope_Complete_DataSaved()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);

            // Act
            tool.StartTransactionScope();
            tool.ExecuteNonQuery($"INSERT INTO {_TableName} (Name) VALUES ('ScopeCommit')");
            tool.CommitScope();
            // 注意：Scope 需要在 Dispose 時才會真正結束，這裡模擬 using 結束
            tool.Dispose();

            // Assert (使用新的連線檢查)
            using var checker = new SqlTool(_ConnStr);
            var count = checker.ExecuteScalar($"SELECT COUNT(*) FROM {_TableName}");
            Assert.Equal(1, count);
        }

        [Fact]
        public void TransactionScope_NoComplete_DataRolledBack()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);

            // Act
            tool.StartTransactionScope();
            tool.ExecuteNonQuery($"INSERT INTO {_TableName} (Name) VALUES ('ScopeRollback')");
            // 故意不呼叫 CommitScope()
            tool.Dispose();

            // Assert
            using var checker = new SqlTool(_ConnStr);
            var count = checker.ExecuteScalar($"SELECT COUNT(*) FROM {_TableName}");
            Assert.Equal(0, count);
        }

        #endregion

        #region Edge Cases / Validation Tests

        [Fact]
        public void StartTransactionScope_WhenConnectionAlreadyOpen_ThrowsException()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);
            tool.OpenSharedConnection(); // 先開連線

            // Act
            Action act = () => tool.StartTransactionScope();


            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("連線已開啟", exception.Message);
        }

        [Fact]
        public void StartTransactionScope_WhenManualTransactionExists_ThrowsException()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);
            tool.BeginTransaction();

            // Act
            Action act = () => tool.StartTransactionScope();

            // Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("已在手動 SqlTransaction 模式中", exception.Message);
        }

        [Fact]
        public void BeginTransaction_WhenScopeExists_ThrowsException()
        {
            // Arrange
            using var tool = new SqlTool(_ConnStr);
            tool.StartTransactionScope();

            // Act
            Action act = () => tool.BeginTransaction();

            // Assert
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("已在 TransactionScope 模式中", exception.Message);
        }

        #endregion
    }
}