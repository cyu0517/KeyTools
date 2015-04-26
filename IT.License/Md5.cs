using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace IT.License
{
    public sealed class Md5
    {
        private static string ByteArrayToHexString(IEnumerable<byte> buf)
        {
            return buf.Aggregate("", (current, b) => current + b.ToString("X"));
        }

        public static string GetFileMd5(string filePath)
        {
            var result = "";

            if (File.Exists(filePath))
            {
                var fileBinary = File.ReadAllBytes(filePath);
                var provider = new MD5CryptoServiceProvider();
                var resultByteArray = provider.ComputeHash(fileBinary);

                result = ByteArrayToHexString(resultByteArray);
            }

            return result.ToLower();
        }

        public static string GetStringMd5(string str)
        {
            var result = "";

            if (!string.IsNullOrEmpty(str))
            {
                var strBinary = System.Text.Encoding.Default.GetBytes(str);
                var provider = new MD5CryptoServiceProvider();
                var resultByteArray = provider.ComputeHash(strBinary);

                result = ByteArrayToHexString(resultByteArray);
            }

            return result.ToLower();
        }
    }
}
