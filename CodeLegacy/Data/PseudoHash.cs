using System;
using System.IO;
using System.Text;

namespace Flowframes.Data
{
    internal class PseudoHash
    {
        public static string GetHash(string path, bool b64 = true)
        {
            var file = new FileInfo(path);
            string hash = $"{file.Name}{file.Length}{file.LastWriteTime.ToString("yyyyMMddHHmmss")}";
            return b64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(hash)).TrimEnd('=') : hash;
        }
    }
}
