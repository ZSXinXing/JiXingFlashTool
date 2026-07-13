using HandyControl.Controls;
using JiXingFlashTool.Entitys;
using JiXingFlashTool.Model;
using JiXingFlashTool.Repositorys;
using SQLite;
using SQLCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    public class SQLService
    {
        private SQLiteConnection _connection;
        private readonly string _databaseName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.sqlite3");
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized;

        /// <summary>
        /// 初始化 SQLite 连接，供现有设备信息读写使用。
        /// </summary>
        private SQLService()
        {
            SQLiteConnectionString options = new SQLiteConnectionString(_databaseName, false);
            _connection = new SQLiteConnection(options);
        }

        /// <summary>
        /// SQL 服务单例。
        /// </summary>
        public static SQLService Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly SQLService instance = new SQLService();
        }

        /// <summary>
        /// 现有 SQLite 连接，用于兼容设备信息等原有存储逻辑。
        /// </summary>
        public SQLiteConnection Conn
        {
            get
            {
                return _connection;
            }
        }

        /// <summary>
        /// 初始化统一的 SQLCore 数据库上下文，并注册资源文件路径仓储。
        /// </summary>
        /// <returns>数据库初始化任务。</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await _initializeLock.WaitAsync();
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                await DataBootstrap.InitAsync(
                    _databaseName,
                    new[] { typeof(ResourceFilePathEntity).Assembly },
                    repositories => repositories.Register(context => new ResourceFilePathRepository(context)));
                _isInitialized = true;
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        /// <summary>
        /// 创建旧设备信息表。
        /// </summary>
        public void CreateTable()
        {
            try
            {
                Conn.CreateTable<EthernetSNModel>();
            }
            catch(Exception ex) {
                Growl.Error(ex.Message);
            }
        }

        /// <summary>
        /// 初始化旧数据库服务入口，保留现有调用兼容性。
        /// </summary>
        public void Init()
        {
          //  CreateTable();
        }

        
    }

}
