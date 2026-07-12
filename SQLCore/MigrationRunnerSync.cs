using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLCore
{
    public interface IMigrationSync
    {
        int Version { get; }
        void Up(SQLiteConnection db);        // 同步执行（tran.Execute / CreateTable 等）
    }

    public sealed class MigrationRunnerSync
    {
        private readonly SQLiteAsyncConnection _db;
        public MigrationRunnerSync(SQLiteAsyncConnection db) => _db = db;

        public async Task<int> GetUserVersionAsync()
        {
            var result = await _db.QueryScalarsAsync<int>("PRAGMA user_version;");
            return result.FirstOrDefault();
        }

        public Task SetUserVersionAsync(int version)
            => _db.ExecuteAsync($"PRAGMA user_version = {version};");

        public async Task MigrateAsync(params IMigrationSync[] migrations)
        {
            if (migrations == null || migrations.Length == 0) return;

            var current = await GetUserVersionAsync();
            var ordered = migrations.OrderBy(m => m.Version).ToArray();

            foreach (var m in ordered)
            {
                if (m.Version <= current) continue;

                await _db.RunInTransactionAsync(tran =>
                {
                    m.Up(tran);
                    // 迁移成功才升级版本
                    tran.Execute($"PRAGMA user_version = {m.Version};");
                });

                current = m.Version;
            }
        }
    }
}
