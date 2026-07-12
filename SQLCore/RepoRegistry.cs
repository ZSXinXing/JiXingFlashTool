using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLCore
{
    public sealed class RepoRegistry
    {
        private readonly DbContext _ctx;
        private readonly ConcurrentDictionary<Type, object> _repos = new();

        public RepoRegistry(DbContext ctx) => _ctx = ctx;

        public void Register<TRepo>(Func<DbContext, TRepo> factory) where TRepo : class
        {
            _repos[typeof(TRepo)] = factory(_ctx);
        }

        public TRepo Get<TRepo>() where TRepo : class
        {
            if (_repos.TryGetValue(typeof(TRepo), out var repo))
                return (TRepo)repo;

            throw new InvalidOperationException($"Repository not registered: {typeof(TRepo).Name}");
        }
    }
}
