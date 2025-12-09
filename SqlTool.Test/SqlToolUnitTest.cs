using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace SeanTool.CSharp.Net8.Test
{
    // 1. 定義測試資料模型
    [TableName("TestUsers")]
    public class TestUser
    {
        [Identity]
        public int Id { get; set; }
        [PrimaryKey]
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // 2. 定義 Fixture (負責資料庫連線字串與基礎環境)
    public class DatabaseFixture : IDisposable
    {
        // 連線字串 (請根據實際環境調整)
        // @"Server=(localdb)\MSSQLLocalDB;Database=TestDB;User Id=你的帳號;Password=你的密碼;TrustServerCertificate=True;"
        public string ConnectionString { get; } = @"Server=(localdb)\MSSQLLocalDB;Database=TestDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public DatabaseFixture()
        {
            // 初始化主要測試表 (給 Model 測試用)
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    IF OBJECT_ID('TestUsers', 'U') IS NOT NULL DROP TABLE TestUsers;
                    CREATE TABLE TestUsers (
                        Id INT IDENTITY(1,1),
                        UserId INT NOT NULL,
                        UserName NVARCHAR(50),
                        Age INT,
                        CreatedAt DATETIME2,
                        CONSTRAINT PK_TestUsers PRIMARY KEY (UserId)
                    );";
                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            // 清理主要測試表
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string sql = "IF OBJECT_ID('TestUsers', 'U') IS NOT NULL DROP TABLE TestUsers;";
                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    // 3. 完整的測試類別
    public class SqlToolCompleteTests : IClassFixture<DatabaseFixture>, IDisposable
    {
        private readonly DatabaseFixture _fixture;
        private readonly IServiceProvider _serviceProvider;
        
        // 專門用於 Ad-hoc 測試的動態 Table 名稱
        private readonly string _tempTableName;

        public SqlToolCompleteTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            
            // --- DI 設置 ---
            var services = new ServiceCollection();
            services.AddSqlTool(_fixture.ConnectionString);
            _serviceProvider = services.BuildServiceProvider();

            // --- Ad-hoc 測試準備 ---
            // 每個測試方法執行前，產生一個隨機 Table 名稱
            _tempTableName = $"TestTable_{Guid.NewGuid().ToString("N")}";
            CreateTempTable();
        }

        // 每個測試結束後清理動態 Table
        public void Dispose()
        {
            try
            {
                using ISqlTool tool = GetSqlTool();
                tool.ExecuteNonQuery($"DROP TABLE IF EXISTS {_tempTableName}");
            }
            catch { /* 忽略清理錯誤 */ }
        }

        private void CreateTempTable()
        {
            using var tool = GetSqlTool();
            string sql = $@"CREATE TABLE {_tempTableName} (
                            Id INT PRIMARY KEY IDENTITY, 
                            Name NVARCHAR(50), 
                            Age INT
                          )";
            tool.ExecuteNonQuery(sql);
        }

        // 輔助方法：從 DI 取得 ISqlTool
        private ISqlTool GetSqlTool()
        {
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ISqlTool>();
        }

        #region DI & Basic Logic Tests (Original + Yours)

        [Fact(DisplayName = "DI: 應成功注入 ISqlTool")]
        public void DependencyInjection_Should_Resolve_SqlTool()
        {
            using var scope = _serviceProvider.CreateScope();
            var sqlTool = scope.ServiceProvider.GetService<ISqlTool>();
            Assert.NotNull(sqlTool);
            Assert.IsType<SqlTool>(sqlTool);
        }

        [Fact(DisplayName = "Core: Select 1 應回傳 1")]
        public void ExecuteScalar_SelectOne_ReturnsOne()
        {
            using var tool = GetSqlTool();
            var result = tool.ExecuteScalar("SELECT 1");
            Assert.Equal(1, result);
        }

        [Fact(DisplayName = "Core: 匿名物件參數 Insert")]
        public void ExecuteNonQuery_InsertWithAnonymousObject_InsertsData()
        {
            using var tool = GetSqlTool();
            var param = new { Name = "Sean", Age = 30 };
            string sql = $"INSERT INTO {_tempTableName} (Name, Age) VALUES (@Name, @Age)";

            int affected = tool.ExecuteNonQuery(sql, param);

            Assert.Equal(1, affected);
            var count = tool.ExecuteScalar($"SELECT COUNT(*) FROM {_tempTableName}");
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "Core: Dictionary 參數 Insert")]
        public void ExecuteNonQuery_InsertWithDictionary_InsertsData()
        {
            using var tool = GetSqlTool();
            var param = new Dictionary<string, object>
            {
                { "@Name", "TestUser" },
                { "@Age", 99 }
            };
            string sql = $"INSERT INTO {_tempTableName} (Name, Age) VALUES (@Name, @Age)";

            tool.ExecuteNonQuery(sql, param);

            var name = tool.ExecuteScalar($"SELECT Name FROM {_tempTableName}");
            Assert.Equal("TestUser", name);
        }

        [Fact(DisplayName = "Core: GetDataTable 應回傳正確資料")]
        public void GetDataTable_ReturnsCorrectData()
        {
            using var tool = GetSqlTool();
            tool.ExecuteNonQuery($"INSERT INTO {_tempTableName} (Name) VALUES ('Row1'), ('Row2')");

            DataTable dt = tool.GetDataTable($"SELECT * FROM {_tempTableName}");

            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal("Row1", dt.Rows[0]["Name"]);
        }

        #endregion

        #region Transaction Tests (Manual & Conflicts)

        [Fact(DisplayName = "Trans: Rollback 資料不應存入")]
        public void BeginTransaction_Rollback_DataNotSaved()
        {
            using var tool = GetSqlTool();
            tool.BeginTransaction();
            tool.ExecuteNonQuery($"INSERT INTO {_tempTableName} (Name) VALUES ('RollbackTarget')");
            tool.Rollback();

            var count = tool.ExecuteScalar($"SELECT COUNT(*) FROM {_tempTableName}");
            Assert.Equal(0, count);
        }

        [Fact(DisplayName = "Trans: Commit 資料應存入")]
        public void BeginTransaction_Commit_DataSaved()
        {
            using var tool = GetSqlTool();
            tool.BeginTransaction();
            tool.ExecuteNonQuery($"INSERT INTO {_tempTableName} (Name) VALUES ('CommitTarget')");
            tool.Commit();

            var count = tool.ExecuteScalar($"SELECT COUNT(*) FROM {_tempTableName}");
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "Conflict: 已開啟連線時不可開 Scope")]
        public void StartTransactionScope_WhenConnectionAlreadyOpen_ThrowsException()
        {
            using var tool = GetSqlTool();
            tool.OpenSharedConnection(); 

            Action act = () => tool.StartTransactionScope();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("連線已開啟", exception.Message);
        }

        [Fact(DisplayName = "Conflict: 已有手動交易時不可開 Scope")]
        public void StartTransactionScope_WhenManualTransactionExists_ThrowsException()
        {
            using var tool = GetSqlTool();
            tool.BeginTransaction();

            Action act = () => tool.StartTransactionScope();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("已在手動 SqlTransaction 模式中", exception.Message);
        }

        [Fact(DisplayName = "Conflict: 已有 Scope 時不可開手動交易")]
        public void BeginTransaction_WhenScopeExists_ThrowsException()
        {
            using var tool = GetSqlTool();
            tool.StartTransactionScope();

            Action act = () => tool.BeginTransaction();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("已在 TransactionScope 模式中", exception.Message);
        }

        #endregion

        #region TransactionScope Tests

        [Fact(DisplayName = "Scope: Complete 資料應存入")]
        public void TransactionScope_Complete_DataSaved()
        {
            using var tool = GetSqlTool();
            
            tool.StartTransactionScope();
            tool.ExecuteNonQuery($"INSERT INTO {_tempTableName} (Name) VALUES ('ScopeCommit')");
            tool.CommitScope();
            // 釋放 Scope
            tool.Dispose();

            // 驗證
            using var checker = GetSqlTool();
            var count = checker.ExecuteScalar($"SELECT COUNT(*) FROM {_tempTableName}");
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "Scope: 未 Complete 資料應回滾")]
        public void TransactionScope_NoComplete_DataRolledBack()
        {
            using var tool = GetSqlTool();

            tool.StartTransactionScope();
            tool.ExecuteNonQuery($"INSERT INTO {_tempTableName} (Name) VALUES ('ScopeRollback')");
            // 故意不呼叫 CommitScope
            tool.Dispose();

            // 驗證
            using var checker = GetSqlTool();
            var count = checker.ExecuteScalar($"SELECT COUNT(*) FROM {_tempTableName}");
            Assert.Equal(0, count);
        }

        #endregion

        #region Model & Bulk Tests (Using TestUsers Table)

        [Fact(DisplayName = "Model: SingleInsert 與 ExecuteScalar")]
        public void Insert_Should_Add_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 101, UserName = "Sean", CreatedAt = DateTime.Now };

            int rowsAffected = tool.SingleInsert(user);

            Assert.Equal(1, rowsAffected);
            object? result = tool.ExecuteScalar("SELECT UserName FROM TestUsers WHERE UserId = @UserId", new { UserId = 101 });
            Assert.Equal("Sean", result?.ToString());
        }

        [Fact(DisplayName = "Model: BulkInsert")]
        public void BulkInsert_Should_Insert_Multiple_Rows()
        {
            using var tool = GetSqlTool();
            var users = new List<TestUser>();
            for (int i = 0; i < 50; i++)
            {
                users.Add(new TestUser { UserId = 1000 + i, UserName = $"Bulk_{i}", CreatedAt = DateTime.Now });
            }

            // Bulk 需要 OpenSharedConnection
            tool.OpenSharedConnection();
            tool.BeginTransaction();
            tool.BulkInsert(users);
            tool.Commit();

            var count = tool.ExecuteScalar("SELECT COUNT(*) FROM TestUsers WHERE UserId >= 1000");
            Assert.Equal(50, Convert.ToInt32(count));
        }

        [Fact(DisplayName = "Model: SingleUpdate")]
        public void Update_Should_Modify_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 201, UserName = "Original", CreatedAt = DateTime.Now };
            tool.SingleInsert(user);

            user.UserName = "Updated";
            tool.SingleUpdate(user);

            var dt = tool.GetDataTable("SELECT UserName FROM TestUsers WHERE UserId = 201");
            Assert.Equal("Updated", dt.Rows[0]["UserName"]);
        }

        [Fact(DisplayName = "Model: Delete")]
        public void Delete_Should_Remove_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 301, UserName = "DeleteMe", CreatedAt = DateTime.Now };
            tool.SingleInsert(user);

            tool.Delete(user);

            var count = tool.ExecuteScalar("SELECT COUNT(*) FROM TestUsers WHERE UserId = 301");
            Assert.Equal(0, Convert.ToInt32(count));
        }

        #endregion
    }
}