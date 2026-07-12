using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// ROM 后端 data 字段解密服务，集中维护对称加密 key 与兼容性解密策略。
    /// </summary>
    public sealed class RomApiDataDecryptor
    {
        private const string EncryptKey = "c93b099c-4e4f-4c9f-af4c-6b3b9c0cc843";
        private static readonly Lazy<RomApiDataDecryptor> LazyInstance = new Lazy<RomApiDataDecryptor>(() => new RomApiDataDecryptor());
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// 全局 ROM 接口 data 解密服务实例。
        /// </summary>
        public static RomApiDataDecryptor Instance => LazyInstance.Value;

        /// <summary>
        /// 解密接口返回的 data 字段。
        /// </summary>
        /// <param name="encryptedData">服务端返回的加密 data 文本，通常为 Base64 或 Hex。</param>
        /// <returns>解密后的明文字符串。</returns>
        public string DecryptData(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData))
            {
                return string.Empty;
            }

            var cipherBytes = TryReadCipherBytes(encryptedData.Trim());
            foreach (var plainText in EnumerateAesPlainTexts(cipherBytes))
            {
                if (IsLikelyPlainText(plainText))
                {
                    return plainText;
                }
            }

            throw new CryptographicException("ROM 接口 data 解密失败，请确认后端加密算法、模式、IV 与密文格式。");
        }

        /// <summary>
        /// 尝试读取密文字节，优先按 Base64 解析，失败后兼容 Hex 字符串。
        /// </summary>
        /// <param name="encryptedData">加密 data 文本。</param>
        /// <returns>密文字节数组。</returns>
        private static byte[] TryReadCipherBytes(string encryptedData)
        {
            try
            {
                return Convert.FromBase64String(encryptedData);
            }
            catch (FormatException)
            {
                if (encryptedData.Length % 2 != 0 || encryptedData.Any(c => !Uri.IsHexDigit(c)))
                {
                    throw;
                }

                var bytes = new byte[encryptedData.Length / 2];
                for (var index = 0; index < bytes.Length; index++)
                {
                    bytes[index] = Convert.ToByte(encryptedData.Substring(index * 2, 2), 16);
                }

                return bytes;
            }
        }

        /// <summary>
        /// 根据常见 AES key 派生方式和 IV 传递方式枚举可能的明文。
        /// </summary>
        /// <param name="cipherBytes">密文字节。</param>
        /// <returns>可能解密成功的明文集合。</returns>
        private static IEnumerable<string> EnumerateAesPlainTexts(byte[] cipherBytes)
        {
            foreach (var keyBytes in EnumerateKeyBytes())
            {
                foreach (var plainText in TryDecryptWithKey(cipherBytes, keyBytes))
                {
                    yield return plainText;
                }
            }
        }

        /// <summary>
        /// 枚举常见的 key 派生形式，兼容后端使用 GUID 原始字节、MD5 或 SHA256 派生 AES key。
        /// </summary>
        /// <returns>AES key 字节集合。</returns>
        private static IEnumerable<byte[]> EnumerateKeyBytes()
        {
            var rawKeyBytes = Encoding.UTF8.GetBytes(EncryptKey);
            if (rawKeyBytes.Length == 16 || rawKeyBytes.Length == 24 || rawKeyBytes.Length == 32)
            {
                yield return rawKeyBytes;
            }

            if (Guid.TryParse(EncryptKey, out var keyGuid))
            {
                yield return keyGuid.ToByteArray();
            }

            using (var md5 = MD5.Create())
            {
                yield return md5.ComputeHash(rawKeyBytes);
            }

            using (var sha256 = SHA256.Create())
            {
                yield return sha256.ComputeHash(rawKeyBytes);
            }
        }

        /// <summary>
        /// 使用指定 key 兼容尝试 AES-CBC 和 AES-ECB 解密。
        /// </summary>
        /// <param name="cipherBytes">密文字节。</param>
        /// <param name="keyBytes">AES key 字节。</param>
        /// <returns>可能解密成功的明文集合。</returns>
        private static IEnumerable<string> TryDecryptWithKey(byte[] cipherBytes, byte[] keyBytes)
        {
            if (cipherBytes.Length > 16)
            {
                var ivBytes = cipherBytes.Take(16).ToArray();
                var payloadBytes = cipherBytes.Skip(16).ToArray();
                var plainText = TryDecryptAes(payloadBytes, keyBytes, ivBytes, CipherMode.CBC);
                if (plainText != null)
                {
                    yield return plainText;
                }
            }

            var zeroIvPlainText = TryDecryptAes(cipherBytes, keyBytes, new byte[16], CipherMode.CBC);
            if (zeroIvPlainText != null)
            {
                yield return zeroIvPlainText;
            }

            var ecbPlainText = TryDecryptAes(cipherBytes, keyBytes, new byte[16], CipherMode.ECB);
            if (ecbPlainText != null)
            {
                yield return ecbPlainText;
            }
        }

        /// <summary>
        /// 按指定 AES 参数执行单次解密，失败时返回 null 供下一种策略继续尝试。
        /// </summary>
        /// <param name="cipherBytes">密文字节。</param>
        /// <param name="keyBytes">AES key 字节。</param>
        /// <param name="ivBytes">AES IV 字节。</param>
        /// <param name="cipherMode">AES 加密模式。</param>
        /// <returns>解密后的明文，失败返回 null。</returns>
        private static string TryDecryptAes(byte[] cipherBytes, byte[] keyBytes, byte[] ivBytes, CipherMode cipherMode)
        {
            if (cipherBytes.Length == 0 || cipherBytes.Length % 16 != 0)
            {
                return null;
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = ivBytes;
                    aes.Mode = cipherMode;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return StrictUtf8.GetString(plainBytes);
                    }
                }
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }

        /// <summary>
        /// 判断解密结果是否像可用明文，避免错误 key 产生乱码时被误用。
        /// </summary>
        /// <param name="plainText">待检查的明文。</param>
        /// <returns>明文是否可接受。</returns>
        private static bool IsLikelyPlainText(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return false;
            }

            var trimmedText = plainText.TrimStart();
            if (trimmedText.StartsWith("{", StringComparison.Ordinal) || trimmedText.StartsWith("[", StringComparison.Ordinal))
            {
                return true;
            }

            return plainText.All(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t');
        }
    }
}
