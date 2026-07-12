using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLCore
{
    public interface IMigration
    {
        int Version { get; }                 // 递增：1,2,3...
        Task UpAsync(SQLiteAsyncConnection db);
    }

    public sealed class MigrationRunner
    {
        private readonly SQLiteAsyncConnection _db;
        public MigrationRunner(SQLiteAsyncConnection db) => _db = db;

        public async Task<int> GetUserVersionAsync()
        {
            // sqlite-net: QueryScalarsAsync<int> 返回 List<int>
            var result = await _db.QueryScalarsAsync<int>("PRAGMA user_version;");
            return result.FirstOrDefault();
        }

        public Task SetUserVersionAsync(int version)
            => _db.ExecuteAsync($"PRAGMA user_version = {version};");

        public async Task MigrateAsync(params IMigration[] migrations)
        {
            if (migrations == null || migrations.Length == 0) return;

            var current = await GetUserVersionAsync();
            var ordered = migrations.OrderBy(m => m.Version).ToArray();

            foreach (var m in ordered)
            {
                if (m.Version <= current) continue;

                // 每个版本一个事务
                await _db.RunInTransactionAsync(tran =>
                {
                    // 注意：RunInTransactionAsync 的 action 不是 async
                    // 所以这里用同步执行 SQL 的方式，或把迁移 UpAsync 里只写 ExecuteAsync 的同步版本
                    // 这里给你一个折中：在 UpAsync 内部只用 _db.ExecuteAsync 会变成 async。
                    // 工程上更建议：迁移提供 Action<SQLiteConnection> 的同步版本。
                    throw new NotSupportedException(
                        "Use MigrationRunner.MigrateSync(...) OR implement IMigrationSync for synchronous migrations.");
                });
            }
        }
    }
}
