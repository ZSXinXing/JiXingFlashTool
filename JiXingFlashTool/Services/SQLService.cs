using HandyControl.Controls;
using JiXingFlashTool.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    public class SQLService
    {
        private SQLiteConnection _connection;
        private string databaseName = $"db.sqlite3";
        private SQLService()
        {
            SQLiteConnectionString options = new SQLiteConnectionString(databaseName, false);
            _connection = new SQLiteConnection(options);
        }
        public static SQLService Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly SQLService instance = new SQLService();
        }

        public SQLiteConnection Conn
        {
            get
            {
                return _connection;
            }
        }

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

        public void Init()
        {
          //  CreateTable();
        }

        
    }

}
