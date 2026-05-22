using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageCore.Model
{
    /// <summary>
    /// 多语言信息
    /// </summary>
    public class LangInfo
    {
        /// <summary>
        /// 语言名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 语言的key
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 是否默认
        /// </summary>
        public bool IsDefault { get; set; } = false;
        public LangInfo(string name, string key, bool isDefault) {
            Name = name;
            Key = key;
            IsDefault = isDefault;
        }
    }
}
