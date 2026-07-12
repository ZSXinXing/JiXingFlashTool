using JiXingFlashTool.Entitys;
using SQLCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiXingFlashTool.Repositorys
{
    /// <summary>
    /// 资源文件路径仓储，负责资源路径表的读取与事务替换。
    /// </summary>
    public sealed class ResourceFilePathRepository : RepositoryBase<ResourceFilePathEntity>
    {
        /// <summary>
        /// 初始化资源文件路径仓储。
        /// </summary>
        /// <param name="context">SQLCore 数据库上下文。</param>
        public ResourceFilePathRepository(DbContext context) : base(context)
        {
        }

        /// <summary>
        /// 使用事务替换全部资源文件路径记录。
        /// </summary>
        /// <param name="entities">需要保存的资源路径实体集合。</param>
        /// <returns>异步保存任务。</returns>
        public Task ReplaceAllAsync(IEnumerable<ResourceFilePathEntity> entities)
        {
            List<ResourceFilePathEntity> pathEntities = (entities ?? Enumerable.Empty<ResourceFilePathEntity>()).ToList();
            return Ctx.RunInTransactionAsync(connection =>
            {
                connection.DeleteAll<ResourceFilePathEntity>();
                foreach (ResourceFilePathEntity entity in pathEntities)
                {
                    connection.Insert(entity);
                }
            });
        }
    }
}
