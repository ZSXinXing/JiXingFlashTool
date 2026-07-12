# SQLCore

## 1. 定位

`SQLCore` 是一个基于 `sqlite-net-pcl` 的轻量级 SQLite 数据访问封装库，面向 `.NET Framework 4.8`。

它提供这些基础能力：

- `DbContext`：数据库连接初始化、自动建表、仓储注册入口
- `RepositoryBase<TEntity>`：通用 CRUD 基类
- `RepoRegistry`：仓储注册与获取
- `UnitOfWork`：事务执行入口
- `MigrationRunnerSync`：推荐使用的同步迁移执行器
- `DataBootstrap`：静态初始化便捷入口

## 2. 外部怎么使用

### 2.1 引用方式

1. 将 `SQLCore.csproj` 加入你的解决方案。
2. 使用 NuGet 还原 `packages.config` 依赖。
3. 在你的业务项目里定义：
   - `Model`：给应用层使用
   - `Entitys`：给数据库持久化使用

### 2.2 职责边界

这个边界需要严格保持：

- `Model` 是应用使用的对象
- `Entitys` 是存到数据库的对象
- 不要让 UI、服务、任务流程直接把 `Entitys` 当业务对象到处传
- 不要把 `Model` 直接拿去做 SQLite 表映射

推荐目录结构：

```text
YourProject
├─ Models
│  └─ UserModel.cs
├─ Entitys
│  └─ UserEntity.cs
├─ Repositorys
│  └─ UserRepository.cs
└─ Extensions
   └─ UserMappingExtensions.cs
```

### 2.3 定义 Entitys

`Entitys` 是数据库结构映射对象，应该带 SQLite 标记，字段设计优先考虑存储稳定性。

```csharp
using SQLite;

namespace Demo.Entitys
{
    [Table("users")]
    public sealed class UserEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string UserName { get; set; }

        public string DisplayName { get; set; }

        public long CreatedAtUnix { get; set; }
    }
}
```

### 2.4 定义 Model

`Model` 是应用层对象，按业务语义组织，不承担数据库映射职责。

```csharp
using System;

namespace Demo.Models
{
    public sealed class UserModel
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string DisplayName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
```

### 2.5 Model 与 Entitys 相互转换实例

推荐单独写映射扩展方法，避免把数据库细节散落到业务代码里。

```csharp
using System;
using Demo.Entitys;
using Demo.Models;

namespace Demo.Extensions
{
    public static class UserMappingExtensions
    {
        public static UserModel ToModel(this UserEntity entity)
        {
            if (entity == null) return null;

            return new UserModel
            {
                Id = entity.Id,
                UserName = entity.UserName,
                DisplayName = entity.DisplayName,
                CreatedAt = DateTimeOffset
                    .FromUnixTimeSeconds(entity.CreatedAtUnix)
                    .LocalDateTime
            };
        }

        public static UserEntity ToEntity(this UserModel model)
        {
            if (model == null) return null;

            return new UserEntity
            {
                Id = model.Id,
                UserName = model.UserName,
                DisplayName = model.DisplayName,
                CreatedAtUnix = new DateTimeOffset(model.CreatedAt).ToUnixTimeSeconds()
            };
        }

        public static void ApplyModel(this UserEntity entity, UserModel model)
        {
            entity.UserName = model.UserName;
            entity.DisplayName = model.DisplayName;
            entity.CreatedAtUnix = new DateTimeOffset(model.CreatedAt).ToUnixTimeSeconds();
        }
    }
}
```

说明：

- `ToModel()`：数据库对象转应用对象
- `ToEntity()`：应用对象转数据库对象
- `ApplyModel()`：更新场景下，把 `Model` 数据回填到已有 `Entity`

### 2.6 定义 Repository

```csharp
using Demo.Entitys;
using SQLCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Demo.Repositorys
{
    public sealed class UserRepository : RepositoryBase<UserEntity>
    {
        public UserRepository(DbContext ctx) : base(ctx) { }

        public Task<List<UserEntity>> GetByUserNameAsync(string userName)
            => WhereAsync(x => x.UserName == userName);
    }
}
```

### 2.7 初始化 DbContext

```csharp
using Demo.Entitys;
using Demo.Repositorys;
using SQLCore;
using System.Threading.Tasks;

public static class DatabaseSetup
{
    public static async Task InitAsync()
    {
        var db = new DbContext();
        await db.InitAsync(new DbOptions
        {
            DbPath = @"C:\Data\demo.db",
            EntityAssemblies = new[] { typeof(UserEntity).Assembly },
            RegisterRepos = repos =>
            {
                repos.Register(ctx => new UserRepository(ctx));
            }
        });
    }
}
```

### 2.8 用仓储读写数据

```csharp
var db = new DbContext();
await db.InitAsync(new DbOptions
{
    DbPath = @"C:\Data\demo.db",
    EntityAssemblies = new[] { typeof(Demo.Entitys.UserEntity).Assembly },
    RegisterRepos = repos =>
    {
        repos.Register(ctx => new Demo.Repositorys.UserRepository(ctx));
    }
});

var userRepo = db.Repos.Get<Demo.Repositorys.UserRepository>();

var model = new Demo.Models.UserModel
{
    UserName = "alice",
    DisplayName = "Alice",
    CreatedAt = DateTime.Now
};

await userRepo.InsertAsync(model.ToEntity());

var entity = await userRepo.FindAsync(1);
var appModel = entity.ToModel();
```

### 2.9 事务用法

```csharp
var uow = new UnitOfWork(db);
await uow.RunAsync(tran =>
{
    tran.Insert(new Demo.Entitys.UserEntity
    {
        UserName = "bob",
        DisplayName = "Bob",
        CreatedAtUnix = DateTimeOffset.Now.ToUnixTimeSeconds()
    });
});
```

### 2.10 静态启动方式

如果你希望项目里直接使用全局数据库对象，也可以走 `DataBootstrap`：

```csharp
await DataBootstrap.InitAsync(
    dbPath: @"C:\Data\demo.db",
    entityAssemblies: new[] { typeof(Demo.Entitys.UserEntity).Assembly },
    registerRepos: repos =>
    {
        repos.Register(ctx => new Demo.Repositorys.UserRepository(ctx));
    });

var repo = DataBootstrap.Db.Repos.Get<Demo.Repositorys.UserRepository>();
```

适用场景：

- 小型桌面项目
- 单数据库实例应用

不太适合：

- 多数据库实例
- 单元测试需要隔离上下文
- 强依赖注入容器的应用

## 3. 内部结构说明

### 3.1 文件结构

```text
SQLCore
├─ DataBootstrap.cs
├─ DbContext.cs
├─ MigrationRunner.cs
├─ MigrationRunnerSync.cs
├─ RepoRegistry.cs
├─ RepositoryBase.cs
├─ UnitOfWork.cs
├─ SQLCore.csproj
└─ packages.config
```

### 3.2 内部类说明

#### DbContext

核心数据库上下文。

重要职责：

- 初始化 SQLite 连接
- 自动扫描 `[Table]` 标记实体并建表
- 管理仓储注册器 `Repos`
- 提供事务入口

重要属性：

- `Conn`
  - 类型：`SQLiteAsyncConnection`
  - 说明：真正执行数据库操作的连接对象
  - 注意：未初始化时访问会抛异常

- `Repos`
  - 类型：`RepoRegistry`
  - 说明：仓储注册与获取入口

重要方法：

- `InitAsync(DbOptions options)`
  - 初始化上下文
  - 创建连接
  - 执行迁移
  - 自动建表
  - 注册仓储

- `RunInTransactionAsync(Action<SQLiteConnection> action)`
  - 执行事务
  - 适合一组必须原子提交的数据库操作

#### DbOptions

初始化配置对象。

重要属性：

- `DbPath`
  - 数据库文件路径

- `EntityAssemblies`
  - 用于扫描 `[Table]` 实体类的程序集集合

- `Migrations`
  - 初始化时要执行的迁移集合

- `RegisterRepos`
  - 仓储注册回调

#### RepositoryBase<TEntity>

通用仓储基类。

适用定位：

- 给每个 `Entity` 仓储提供基础 CRUD 能力
- 业务仓储在此基础上继续扩展按字段查询、统计、分页等方法

重要属性：

- `Ctx`
  - 当前仓储绑定的 `DbContext`

- `Conn`
  - 从 `DbContext` 暴露出来的 `SQLiteAsyncConnection`

重要方法：

- `FindAsync(object pk)`
  - 按主键查一条

- `GetAllAsync()`
  - 查询全部

- `WhereAsync(Expression<Func<TEntity, bool>> predicate)`
  - 按条件过滤

- `InsertAsync(TEntity entity)`
  - 插入

- `InsertOrReplaceAsync(TEntity entity)`
  - 插入或替换

- `UpdateAsync(TEntity entity)`
  - 更新

- `DeleteAsync(TEntity entity)`
  - 删除

- `ExecuteAsync(string sql, params object[] args)`
  - 执行自定义 SQL

#### RepoRegistry

仓储注册器。

用途：

- 统一注册自定义仓储
- 避免每次手动 new 仓储
- 作为 `DbContext.Repos` 的访问入口

重要方法：

- `Register<TRepo>(Func<DbContext, TRepo> factory)`
  - 注册仓储工厂

- `Get<TRepo>()`
  - 获取已注册仓储
  - 未注册会抛 `InvalidOperationException`

#### UnitOfWork

事务执行封装。

用途：

- 聚合多个数据库写操作
- 保证事务一致性

重要方法：

- `RunAsync(Action<SQLiteConnection> action)`
  - 在同一事务中执行同步数据库操作

#### DataBootstrap

静态初始化入口。

用途：

- 给不想到处传递 `DbContext` 的应用提供快捷启动方式

重要属性：

- `Db`
  - 全局 `DbContext`

- `Uow`
  - 全局 `UnitOfWork`

重要方法：

- `InitAsync(string dbPath, Assembly[] entityAssemblies, Action<RepoRegistry> registerRepos)`
  - 初始化全局数据库对象

#### IMigration / MigrationRunner

异步迁移接口与执行器。

当前状态说明：

- `IMigration` 定义了异步迁移接口
- `MigrationRunner` 当前在事务内直接抛出 `NotSupportedException`
- 原因是 `RunInTransactionAsync` 的委托本身不是 `async`，不适合直接执行异步迁移逻辑

结论：

- 这个类目前更像“占位设计”
- 生产使用时不建议优先依赖它

#### IMigrationSync / MigrationRunnerSync

推荐使用的同步迁移方案。

用途：

- 用事务串行执行数据库版本升级
- 每个版本升级成功后写入 `PRAGMA user_version`

重要方法：

- `GetUserVersionAsync()`
  - 读取当前数据库版本号

- `SetUserVersionAsync(int version)`
  - 手动设置数据库版本号

- `MigrateAsync(params IMigrationSync[] migrations)`
  - 按 `Version` 顺序执行迁移
  - 只执行版本号大于当前库版本的迁移

## 4. 重要设计约束

### 4.1 Model 与 Entitys 必须分离

强烈建议始终保持：

- `Model` 只服务应用层
- `Entitys` 只服务数据库层
- 仓储返回 `Entitys` 后，在业务层尽快转成 `Model`

不推荐：

```csharp
public async Task<UserEntity> GetCurrentUserAsync()
{
    return await _userRepo.FindAsync(1);
}
```

推荐：

```csharp
public async Task<UserModel> GetCurrentUserAsync()
{
    var entity = await _userRepo.FindAsync(1);
    return entity.ToModel();
}
```

### 4.2 仓储以 Entity 为边界

`RepositoryBase<TEntity>` 的泛型就是数据库实体类型，因此：

- Repository 输入输出天然更偏向 `Entitys`
- 应用服务层自己决定何时映射成 `Model`

### 4.3 自动建表依赖 `[Table]`

`DbContext.AutoCreateTablesAsync()` 会扫描带 `[Table]` 特性的类型。

因此：

- 没有 `[Table]` 的类型不会自动建表
- `Model` 不应该带 `[Table]`
- `Entitys` 应该集中放在可扫描的程序集里

## 5. 建议实践

- 一个表对应一个 `Entity`
- 一个业务对象对应一个 `Model`
- 一个表的持久化逻辑对应一个 Repository
- 映射逻辑统一收口到扩展方法或 Mapper 类
- 多条写入操作通过 `UnitOfWork` 事务执行
- 数据库结构升级优先使用 `MigrationRunnerSync`

## 6. 已知注意事项

- 本库当前目标框架是 `.NET Framework 4.8`
- 依赖 `packages.config` 风格 NuGet 还原
- `MigrationRunner` 异步迁移器当前不适合作为主迁移入口
- `DataBootstrap` 很方便，但会引入全局状态，测试和多实例场景要谨慎
