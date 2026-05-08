using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Utils
{
    /// <summary>
    /// 本地文件工具类。
    /// </summary>
    public class FileUtil
    {
        /// <summary>
        /// 计算文件的 MD5 值。
        /// </summary>
        /// <param name="filePath">文件路径。</param>
        /// <returns>MD5 字符串。</returns>
        public static string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    var result = new StringBuilder(hash.Length * 2);

                    foreach (byte b in hash)
                    {
                        result.Append(b.ToString("x2"));
                    }

                    return result.ToString();
                }
            }
        }

        /// <summary>
        /// 检查指定绝对路径的本地文件是否存在。
        /// </summary>
        /// <param name="absolutePath">本地文件绝对路径。</param>
        /// <returns>存在返回 true，否则返回 false。</returns>
        public static bool IsLocalFileExists(string absolutePath)
        {
            return !string.IsNullOrWhiteSpace(absolutePath) && Path.IsPathRooted(absolutePath) && File.Exists(absolutePath);
        }
    }
}
