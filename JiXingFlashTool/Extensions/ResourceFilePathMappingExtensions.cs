using JiXingFlashTool.Entitys;
using JiXingFlashTool.Model;
using System;

namespace JiXingFlashTool.Extensions
{
    /// <summary>
    /// 资源路径业务模型与数据库实体之间的转换扩展。
    /// </summary>
    public static class ResourceFilePathMappingExtensions
    {
        /// <summary>
        /// 将数据库实体转换为资源路径业务模型。
        /// </summary>
        /// <param name="entity">资源路径数据库实体。</param>
        /// <returns>资源路径业务模型。</returns>
        public static ResourceFilePathModel ToModel(this ResourceFilePathEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new ResourceFilePathModel
            {
                ResourceType = entity.ResourceType,
                Series = entity.Series,
                FilePath = entity.FilePath,
                UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(entity.UpdatedAtUnix).LocalDateTime
            };
        }

        /// <summary>
        /// 将资源路径业务模型转换为数据库实体。
        /// </summary>
        /// <param name="model">资源路径业务模型。</param>
        /// <returns>资源路径数据库实体。</returns>
        public static ResourceFilePathEntity ToEntity(this ResourceFilePathModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new ResourceFilePathEntity
            {
                ResourceKey = $"{model.ResourceType}:{model.Series}",
                ResourceType = model.ResourceType,
                Series = model.Series,
                FilePath = model.FilePath,
                UpdatedAtUnix = new DateTimeOffset(model.UpdatedAt).ToUnixTimeSeconds()
            };
        }
    }
}
