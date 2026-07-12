using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLCore
{
    public sealed class DbContext
    {
        private SQLiteAsyncConnection _conn;
        private bool _inited;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public SQLiteAsyncConnection Conn
            => _conn ?? throw new InvalidOperationException("DbContext not initialized.");

        public RepoRegistry Repos { get; private set; }

        public async Task InitAsync(DbOptions options)
        {
            if (_inited) return;

            await _initLock.WaitAsync();
            try
            {
                if (_inited) return;

                var dbPath = options.DbPath ?? GetDefaultDbPath("db.sqlite3");
                _conn = new SQLiteAsyncConnection(dbPath);

                if (options.Migrations != null)
                {
                    var runner = new MigrationRunner(_conn);
                    await runner.MigrateAsync(options.Migrations);
                }

                var assemblies = options.EntityAssemblies?.Length > 0
                    ? options.EntityAssemblies
                    : new[] { Assembly.GetExecutingAssembly() };

                await AutoCreateTablesAsync(assemblies);


                Repos = new RepoRegistry(this);
                if (options.RegisterRepos != null)
                    options.RegisterRepos(Repos);

                _inited = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public Task RunInTransactionAsync(Action<SQLiteConnection> action)
            => Conn.RunInTransactionAsync(action);

        private async Task AutoCreateTablesAsync(params Assembly[] assemblies)
        {
            var types = assemblies
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
                })
                .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<TableAttribute>() != null)
                .Distinct()
                .ToArray();

            if (types.Length > 0)
                await Conn.CreateTablesAsync(CreateFlags.None, types);
        }

        private static string GetDefaultDbPath(string fileName)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(folder, fileName);
        }
    }

    public sealed class DbOptions
    {
        /// <summary>
        /// 数据库路径
        /// </summary>
        public string DbPath { get; set; }

        /// <summary>
        /// 实体类所在的集合
        /// </summary>
        public Assembly[] EntityAssemblies { get; set; }

        /// <summary>
        /// 需要升级数据
        /// </summary>
        public IMigration[] Migrations { get; set; }

        /// <summary>
        ///  Repo注册，用于大量使用
        /// </summary>
        public Action<RepoRegistry> RegisterRepos { get; set; }
    }
}
