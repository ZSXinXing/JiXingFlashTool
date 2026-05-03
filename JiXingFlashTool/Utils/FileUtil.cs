using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Utils
{
    public class FileUtil
    {
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
    }
}
