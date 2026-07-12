using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLCore
{
    public static class DataBootstrap
    {
        public static DbContext Db { get; private set; }
        public static UnitOfWork Uow { get; private set; }


        public static async Task InitAsync(string dbPath, Assembly[] entityAssemblies, Action<RepoRegistry> registerRepos)
        {
            Db = new DbContext();
            await Db.InitAsync(new DbOptions
            {
                DbPath = dbPath,
                EntityAssemblies = entityAssemblies,
                RegisterRepos = registerRepos
            });

            Uow = new UnitOfWork(Db);
        }
    }
}
