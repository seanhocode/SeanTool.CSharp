using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

// 強制關閉平行測試，讓測試一個接一個跑
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace SeanTool.CSharp.Test
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

    public class DatabaseFixture : IDisposable
    {
        // 連線字串 (請根據實際環境調整)
        // @"Server=(localdb)\MSSQLLocalDB;Database=TestDB;User Id=你的帳號;Password=你的密碼;TrustServerCertificate=True;";
        // @"Server=(localdb)\MSSQLLocalDB;Database=TestDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public string ConnectionString { get; } = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

        public DatabaseFixture()
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                // --- 修改這裡 ---
                // 使用更嚴謹的 SQL：先刪除，再檢查是否真的不存在才建立
                string sql = @"
                    -- 1. 如果存在則刪除
                    IF OBJECT_ID('TestUsers', 'U') IS NOT NULL 
                        DROP TABLE TestUsers;

                    -- 2. 只有在確認不存在時才建立 (防止多執行緒同時建立導致錯誤)
                    IF OBJECT_ID('TestUsers', 'U') IS NULL
                    BEGIN
                        CREATE TABLE TestUsers (
                            Id INT IDENTITY(1,1),
                            UserId INT NOT NULL,
                            UserName NVARCHAR(50),
                            Age INT,
                            CreatedAt DATETIME2,
                            CONSTRAINT PK_TestUsers PRIMARY KEY (UserId)
                        );
                    END";
                // ----------------

                using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "IF OBJECT_ID('TestUsers', 'U') IS NOT NULL DROP TABLE TestUsers;";
                    using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // 忽略 Dispose 時的錯誤，避免掩蓋測試本身的失敗
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
            EnsureStoredProcedures();
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

        private void EnsureStoredProcedures()
        {
            using var tool = GetSqlTool();

            // 1. 查詢用 SP (支援選填參數)
            // 註: CREATE OR ALTER 需要 SQL Server 2016+
            string sqlGet = @"
                CREATE OR ALTER PROCEDURE sp_Test_GetUsers
                    @UserId INT = NULL
                AS
                BEGIN
                    IF @UserId IS NULL
                        SELECT * FROM TestUsers ORDER BY UserId
                    ELSE
                        SELECT * FROM TestUsers WHERE UserId = @UserId
                END";
            tool.ExecuteNonQuery(sqlGet);

            // 2. 新增用 SP (無回傳值)
            string sqlInsert = @"
                CREATE OR ALTER PROCEDURE sp_Test_InsertUser
                    @UserId INT,
                    @UserName NVARCHAR(50),
                    @Age INT
                AS
                BEGIN
                    INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) 
                    VALUES (@UserId, @UserName, @Age, GETDATE())
                END";
            tool.ExecuteNonQuery(sqlInsert);

            // 3. 測試 Output 參數用 SP
            string sqlOutput = @"
                CREATE OR ALTER PROCEDURE sp_Test_CalcOutput
                    @InputVal INT,
                    @OutputVal INT OUTPUT
                AS
                BEGIN
                    SET @OutputVal = @InputVal * 10
                END";
            tool.ExecuteNonQuery(sqlOutput);
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

            DataTable dt = tool.ExecuteSQL($"SELECT * FROM {_tempTableName}");

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
        [Fact(DisplayName = "Core: ExecuteSQL 應正確映射資料至 TestUser 模型")]
        public void ExecuteSQL_Should_Map_To_Model_List()
        {
            using var tool = GetSqlTool();

            // Arrange: 準備測試資料 (使用 TestUsers 表)
            // 這裡刻意插入兩筆資料
            tool.ExecuteNonQuery($"DELETE FROM TestUsers WHERE UserId IN (801, 802)");

            var sqlInsert = @"
                INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) 
                VALUES (801, 'MapperA', 25, '2023-01-01 10:00:00'),
                       (802, 'MapperB', 35, '2023-02-01 11:00:00')";
            tool.ExecuteNonQuery(sqlInsert);

            // Act: 執行泛型查詢
            IList<TestUser> result = tool.ExecuteSQL<TestUser>("SELECT * FROM TestUsers WHERE UserId IN (801, 802) ORDER BY UserId");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            // 驗證第一筆
            var user1 = result[0];
            Assert.Equal(801, user1.UserId);
            Assert.Equal("MapperA", user1.UserName);
            Assert.Equal(25, user1.Age);
            Assert.Equal(new DateTime(2023, 1, 1, 10, 0, 0), user1.CreatedAt);
            // Id 是 Identity 自動生成的，只要大於 0 即可
            Assert.True(user1.Id > 0);

            // 驗證第二筆
            var user2 = result[1];
            Assert.Equal(802, user2.UserId);
            Assert.Equal("MapperB", user2.UserName);
        }

        [Fact(DisplayName = "Core: ExecuteSQL 遇到 DBNull 應保留 Model 預設值")]
        public void ExecuteSQL_Should_Handle_Nulls_By_Preserving_Defaults()
        {
            using var tool = GetSqlTool();

            // Arrange: 插入含有 NULL 的資料
            // UserName 與 Age 在 Table Schema 中允許 NULL
            tool.ExecuteNonQuery($"DELETE FROM TestUsers WHERE UserId = @UserID", new { UserID = 803 });

            var sqlInsert = @"
                INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) 
                VALUES (803, NULL, NULL, '2023-03-01')";
            tool.ExecuteNonQuery(sqlInsert);

            // Act
            IList<TestUser> result = tool.ExecuteSQL<TestUser>("SELECT * FROM TestUsers WHERE UserId = @UserID", new { UserID = 803 });

            // Assert
            Assert.Single(result);
            var user = result[0];

            Assert.Equal(803, user.UserId);

            // 驗證重點：
            // 因為 DB 值是 NULL，轉換邏輯通常會跳過屬性賦值。
            // 所以屬性應維持 Model 定義時的初始值。
            Assert.Equal(string.Empty, user.UserName); // Model 初始化為 string.Empty
            Assert.Equal(0, user.Age);                 // int 預設為 0
        }

        [Fact(DisplayName = "Core: ExecuteSQL 查無資料應回傳空清單")]
        public void ExecuteSQL_Should_Return_Empty_List_When_No_Data()
        {
            using var tool = GetSqlTool();

            // Act: 查詢一個不存在的條件
            IList<TestUser> result = tool.ExecuteSQL<TestUser>("SELECT * FROM TestUsers WHERE UserId = -9999");

            // Assert
            Assert.NotNull(result); // 不應回傳 null
            Assert.Empty(result);   // Count 應為 0
        }

        [Fact(DisplayName = "Model: SingleInsert 與 ExecuteScalar")]
        public void Insert_Should_Add_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 101, UserName = "Sean", CreatedAt = DateTime.Now };

            int rowsAffected = tool.SingleInsert(user);

            Assert.True(rowsAffected > 0);
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
            tool.ExecuteNonQuery("DELETE FROM TestUsers WHERE UserId >= 1000");
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

            var dt = tool.ExecuteSQL("SELECT UserName FROM TestUsers WHERE UserId = @UserId", new { UserId = 201 });
            Assert.Equal("Updated", dt.Rows[0]["UserName"]);
        }

        [Fact(DisplayName = "Model: Delete")]
        public void Delete_Should_Remove_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 301, UserName = "DeleteMe", CreatedAt = DateTime.Now };
            tool.SingleInsert(user);

            tool.Delete(user);

            var count = tool.ExecuteScalar("SELECT COUNT(*) FROM TestUsers WHERE UserId = @UserId", new { UserId = 301 });
            Assert.Equal(0, Convert.ToInt32(count));
        }

        #endregion

        #region Async Tests
        [Fact(DisplayName = "Async Core: Select 1 應回傳 1")]
        public async Task ExecuteScalarAsync_SelectOne_ReturnsOne()
        {
            using var tool = GetSqlTool();
            var result = await tool.ExecuteScalarAsync("SELECT 1");
            Assert.Equal(1, result);
        }

        [Fact(DisplayName = "Async Core: 匿名物件參數 Insert")]
        public async Task ExecuteNonQueryAsync_InsertWithAnonymousObject_InsertsData()
        {
            using var tool = GetSqlTool();
            var param = new { Name = "SeanAsync", Age = 31 };
            string sql = $"INSERT INTO {_tempTableName} (Name, Age) VALUES (@Name, @Age)";

            int affected = await tool.ExecuteNonQueryAsync(sql, param);

            Assert.Equal(1, affected);
            var count = await tool.ExecuteScalarAsync($"SELECT COUNT(*) FROM {_tempTableName} WHERE Name = @Name", new { Name = "SeanAsync" });
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "Async Core: ExecuteSQLAsync 應回傳 DataTable")]
        public async Task ExecuteSQLAsync_ReturnsDataTable()
        {
            using var tool = GetSqlTool();
            await tool.ExecuteNonQueryAsync($"INSERT INTO {_tempTableName} (Name) VALUES (@Row1), (@Row2)", new { Row1 = "AsyncRow1", Row2 = "AsyncRow2" });

            DataTable dt = await tool.ExecuteSQLAsync($"SELECT * FROM {_tempTableName}");

            Assert.Equal(2, dt.Rows.Count);
            Assert.Contains("AsyncRow1", dt.Rows[0]["Name"].ToString());
        }

        [Fact(DisplayName = "Async Core: ExecuteSQLAsync<T> 應映射 Model List")]
        public async Task ExecuteSQLAsync_Generic_ReturnsModelList()
        {
            using var tool = GetSqlTool();

            // Arrange
            await tool.ExecuteNonQueryAsync($"DELETE FROM TestUsers WHERE UserId IN (901, 902)");
            var sqlInsert = @"
                INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) 
                VALUES (901, 'AsyncUserA', 28, GETDATE()),
                       (902, 'AsyncUserB', 38, GETDATE())";
            await tool.ExecuteNonQueryAsync(sqlInsert);

            // Act
            IList<TestUser> result = await tool.ExecuteSQLAsync<TestUser>("SELECT * FROM TestUsers WHERE UserId IN (901, 902) ORDER BY UserId");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("AsyncUserA", result[0].UserName);
            Assert.Equal(902, result[1].UserId);
        }

        [Fact(DisplayName = "Async Trans: Rollback 資料不應存入")]
        public async Task BeginTransactionAsync_Rollback_DataNotSaved()
        {
            using var tool = GetSqlTool();
            await tool.BeginTransactionAsync();
            await tool.ExecuteNonQueryAsync($"INSERT INTO {_tempTableName} (Name) VALUES ('AsyncRollback')");
            await tool.RollbackAsync();

            var count = await tool.ExecuteScalarAsync($"SELECT COUNT(*) FROM {_tempTableName} WHERE Name = 'AsyncRollback'");
            Assert.Equal(0, count);
        }

        [Fact(DisplayName = "Async Trans: Commit 資料應存入")]
        public async Task BeginTransactionAsync_Commit_DataSaved()
        {
            using var tool = GetSqlTool();
            await tool.BeginTransactionAsync();
            await tool.ExecuteNonQueryAsync($"INSERT INTO {_tempTableName} (Name) VALUES ('AsyncCommit')");
            await tool.CommitAsync();

            var count = await tool.ExecuteScalarAsync($"SELECT COUNT(*) FROM {_tempTableName} WHERE Name = 'AsyncCommit'");
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "Async Model: SingleInsertAsync")]
        public async Task SingleInsertAsync_Should_Add_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 401, UserName = "AsyncInsert", CreatedAt = DateTime.Now };

            // 這裡假設您已經修正了 SingleInsertAsync 回傳 ID 的問題，或者是回傳筆數(int)
            int result = await tool.SingleInsertAsync(user);

            Assert.True(result > 0);
            var dbName = await tool.ExecuteScalarAsync("SELECT UserName FROM TestUsers WHERE UserId = 401");
            Assert.Equal("AsyncInsert", dbName?.ToString());
        }

        [Fact(DisplayName = "Async Model: SingleUpdateAsync")]
        public async Task SingleUpdateAsync_Should_Modify_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 402, UserName = "AsyncOriginal", CreatedAt = DateTime.Now };
            await tool.SingleInsertAsync(user);

            user.UserName = "AsyncUpdated";
            await tool.SingleUpdateAsync(user);

            var dbName = await tool.ExecuteScalarAsync("SELECT UserName FROM TestUsers WHERE UserId = 402");
            Assert.Equal("AsyncUpdated", dbName?.ToString());
        }

        [Fact(DisplayName = "Async Model: DeleteAsync")]
        public async Task DeleteAsync_Should_Remove_Record()
        {
            using var tool = GetSqlTool();
            var user = new TestUser { UserId = 403, UserName = "AsyncDelete", CreatedAt = DateTime.Now };
            await tool.SingleInsertAsync(user);

            await tool.DeleteAsync(user);

            var count = await tool.ExecuteScalarAsync("SELECT COUNT(*) FROM TestUsers WHERE UserId = 403");
            Assert.Equal(0, Convert.ToInt32(count));
        }

        [Fact(DisplayName = "Async Model: BulkInsertAsync")]
        public async Task BulkInsertAsync_Should_Insert_Multiple_Rows()
        {
            using var tool = GetSqlTool();
            var users = new List<TestUser>();
            // 建立 50 筆測試資料
            for (int i = 0; i < 50; i++)
            {
                users.Add(new TestUser { UserId = 5000 + i, UserName = $"AsyncBulk_{i}", CreatedAt = DateTime.Now });
            }

            // 清理舊資料避免 PK 衝突
            await tool.ExecuteNonQueryAsync("DELETE FROM TestUsers WHERE UserId >= 5000 AND UserId < 5050");

            // Bulk 需要 OpenSharedConnection
            await tool.OpenSharedConnectionAsync();
            await tool.BeginTransactionAsync();

            await tool.BulkInsertAsync(users);

            await tool.CommitAsync();

            var count = await tool.ExecuteScalarAsync("SELECT COUNT(*) FROM TestUsers WHERE UserId >= 5000 AND UserId < 5050");
            Assert.Equal(50, Convert.ToInt32(count));
        }

        [Fact(DisplayName = "Async Model: BulkUpdateAsync")]
        public async Task BulkUpdateAsync_Should_Update_Multiple_Rows()
        {
            using var tool = GetSqlTool();
            var users = new List<TestUser>();

            // 1. 先插入 10 筆原始資料
            for (int i = 0; i < 10; i++)
            {
                users.Add(new TestUser { UserId = 6000 + i, UserName = $"Original_{i}", CreatedAt = DateTime.Now });
            }
            await tool.ExecuteNonQueryAsync("DELETE FROM TestUsers WHERE UserId >= 6000 AND UserId < 6010");

            await tool.OpenSharedConnectionAsync();
            await tool.BeginTransactionAsync();
            await tool.BulkInsertAsync(users);
            await tool.CommitAsync();

            // 2. 修改記憶體中的資料
            foreach (var u in users)
            {
                u.UserName = u.UserName.Replace("Original", "Updated");
            }

            // 3. 執行 BulkUpdateAsync
            // 注意：BulkUpdate 內部邏輯通常會自動處理連線，但手動開啟更保險
            await tool.BulkUpdateAsync(users);

            // 4. 驗證
            var checkDt = await tool.ExecuteSQLAsync("SELECT UserName FROM TestUsers WHERE UserId >= 6000 AND UserId < 6010");
            foreach (DataRow row in checkDt.Rows)
            {
                Assert.Contains("Updated", row["UserName"].ToString());
            }
        }
        #endregion

        #region Stored Procedure Tests

        [Fact(DisplayName = "SP: ExecuteStoredProcedure (DataTable)")]
        public void ExecuteStoredProcedure_Should_Return_DataTable()
        {
            // Ensure SP exists
            EnsureStoredProcedures();

            using var tool = GetSqlTool();
            // Arrange: 準備資料
            tool.ExecuteNonQuery("DELETE FROM TestUsers WHERE UserId IN (701, 702)");
            tool.ExecuteNonQuery("INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) VALUES (701, 'SP_UserA', 20, GETDATE()), (702, 'SP_UserB', 30, GETDATE())");

            // Act: 執行 SP
            var dt = tool.ExecuteStoredProcedure("sp_Test_GetUsers");

            // Assert
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count >= 2); // 因為可能還有其他測試資料，所以用 >=
            Assert.Contains(dt.AsEnumerable(), r => r["UserName"].ToString() == "SP_UserA");
        }

        [Fact(DisplayName = "SP Async: ExecuteStoredProcedureAsync (DataTable)")]
        public async Task ExecuteStoredProcedureAsync_Should_Return_DataTable()
        {
            EnsureStoredProcedures();

            using var tool = GetSqlTool();
            // Arrange
            await tool.ExecuteNonQueryAsync("DELETE FROM TestUsers WHERE UserId = 703");
            await tool.ExecuteNonQueryAsync("INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) VALUES (703, 'SP_Async', 40, GETDATE())");

            // Act
            var dt = await tool.ExecuteStoredProcedureAsync("sp_Test_GetUsers", new { UserId = 703 });

            // Assert
            Assert.NotNull(dt);
            Assert.Single(dt.Rows);
            Assert.Equal("SP_Async", dt.Rows[0]["UserName"]);
        }

        [Fact(DisplayName = "SP: ExecuteStoredProcedure<T> (Model List)")]
        public void ExecuteStoredProcedure_Generic_Should_Return_ModelList()
        {
            EnsureStoredProcedures();

            using var tool = GetSqlTool();
            // Arrange
            tool.ExecuteNonQuery("DELETE FROM TestUsers WHERE UserId = 704");
            tool.ExecuteNonQuery("INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) VALUES (704, 'SP_Model', 50, GETDATE())");

            // Act
            IList<TestUser> result = tool.ExecuteStoredProcedure<TestUser>("sp_Test_GetUsers", new { UserId = 704 });

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(704, result[0].UserId);
            Assert.Equal("SP_Model", result[0].UserName);
        }

        [Fact(DisplayName = "SP Async: ExecuteStoredProcedureAsync<T> (Model List)")]
        public async Task ExecuteStoredProcedureAsync_Generic_Should_Return_ModelList()
        {
            EnsureStoredProcedures();

            using var tool = GetSqlTool();
            // Arrange
            await tool.ExecuteNonQueryAsync("DELETE FROM TestUsers WHERE UserId = 705");
            await tool.ExecuteNonQueryAsync("INSERT INTO TestUsers (UserId, UserName, Age, CreatedAt) VALUES (705, 'SP_AsyncModel', 60, GETDATE())");

            // Act
            IList<TestUser> result = await tool.ExecuteStoredProcedureAsync<TestUser>("sp_Test_GetUsers", new { UserId = 705 });

            // Assert
            Assert.Single(result);
            Assert.Equal("SP_AsyncModel", result[0].UserName);
        }

        [Fact(DisplayName = "SP: ExecuteStoredProcedureNonQuery (Insert)")]
        public void ExecuteStoredProcedureNonQuery_Should_Insert_Data()
        {
            EnsureStoredProcedures();

            using var tool = GetSqlTool();
            // Arrange
            int newId = 706;
            tool.ExecuteNonQuery("DELETE FROM TestUsers WHERE UserId = @Id", new { Id = newId });
            var param = new { UserId = newId, UserName = "SP_Insert", Age = 25 };

            // Act
            int affected = tool.ExecuteStoredProcedureNonQuery("sp_Test_InsertUser", param);

            // Assert
            Assert.Equal(1, affected);
            var count = tool.ExecuteScalar("SELECT COUNT(*) FROM TestUsers WHERE UserId = @Id", new { Id = newId });
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "SP Async: ExecuteStoredProcedureNonQueryAsync (Insert)")]
        public async Task ExecuteStoredProcedureNonQueryAsync_Should_Insert_Data()
        {
            EnsureStoredProcedures();

            using var tool = GetSqlTool();
            // Arrange
            int newId = 707;
            await tool.ExecuteNonQueryAsync("DELETE FROM TestUsers WHERE UserId = @Id", new { Id = newId });
            var param = new { UserId = newId, UserName = "SP_AsyncInsert", Age = 35 };

            // Act
            int affected = await tool.ExecuteStoredProcedureNonQueryAsync("sp_Test_InsertUser", param);

            // Assert
            Assert.Equal(1, affected);
            var count = await tool.ExecuteScalarAsync("SELECT COUNT(*) FROM TestUsers WHERE UserId = @Id", new { Id = newId });
            Assert.Equal(1, count);
        }

        [Fact(DisplayName = "SP Extra: Output Parameters")]
        public void ExecuteStoredProcedureNonQuery_WithOutputParam_Should_Work()
        {
            // 這是額外測試，驗證您在 AddParameters 加入的 IEnumerable<SqlParameter> 功能是否正常
            EnsureStoredProcedures();

            using var tool = GetSqlTool();

            // Arrange
            var outParam = new SqlParameter("@OutputVal", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@InputVal", 5),
                outParam
            };

            // Act
            tool.ExecuteStoredProcedureNonQuery("sp_Test_CalcOutput", parameters);

            // Assert
            // 預期邏輯是 Output = Input * 10
            Assert.Equal(50, Convert.ToInt32(outParam.Value));
        }

        #endregion
    }
}