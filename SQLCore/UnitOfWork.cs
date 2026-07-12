using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLCore
{
    public sealed class UnitOfWork
    {
        private readonly DbContext _ctx;
        public UnitOfWork(DbContext ctx) => _ctx = ctx;

        public Task RunAsync(Action<SQLiteConnection> action)
            => _ctx.Conn.RunInTransactionAsync(action);
    }
}
