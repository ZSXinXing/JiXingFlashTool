using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// 以太网扫描的设置Model
    /// </summary>
    public class EthernetSNModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string StartIP { get; set; }
        public string EndIP { get; set; }
        public string Port { get; set; } = "5555";
        public string Timeout { get; set; } = "100";

        /// <summary>
        /// 扫描数量
        /// </summary>
        public int SNCount;
        /// <summary>
        /// 连接数量
        /// </summary>
        public int ConnectCount;

        
    }
}
