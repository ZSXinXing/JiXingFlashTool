using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SQLCore
{
    public abstract class RepositoryBase<TEntity> where TEntity : new()
    {
        protected readonly DbContext Ctx;
        protected SQLiteAsyncConnection Conn => Ctx.Conn;

        protected RepositoryBase(DbContext ctx) => Ctx = ctx;

        protected RepositoryBase() {
            Ctx = DataBootstrap.Db;
        }

        public virtual Task<TEntity> FindAsync(object pk) => Conn.FindAsync<TEntity>(pk);

        public virtual Task<List<TEntity>> GetAllAsync() => Conn.Table<TEntity>().ToListAsync();

        public virtual Task<List<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> predicate)
            => Conn.Table<TEntity>().Where(predicate).ToListAsync();

        public virtual Task<int> InsertAsync(TEntity entity) => Conn.InsertAsync(entity);

        public virtual Task<int> InsertOrReplaceAsync(TEntity entity) => Conn.InsertOrReplaceAsync(entity);

        public virtual Task<int> UpdateAsync(TEntity entity) => Conn.UpdateAsync(entity);

        public virtual Task<int> DeleteAsync(TEntity entity) => Conn.DeleteAsync(entity);

        public virtual Task<int> ExecuteAsync(string sql, params object[] args) => Conn.ExecuteAsync(sql, args);
    }
}
