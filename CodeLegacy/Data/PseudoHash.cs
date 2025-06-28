using Flowframes.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Flowframes.Data
{
    internal class PseudoHash
    {
        public static string GetHash(string path, bool b64 = true)
        {
            bool isDir = Directory.Exists(path);

            if (isDir)
            {
                var dir = new DirectoryInfo(path);
                var files = IoUtils.GetFileInfosSorted(path);
                string dirHash = $"{dir.Name}{files.Sum(f => f.Length)}{dir.LastWriteTime.ToString("yyyyMMddHHmmss")}";
                return b64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(dirHash)).TrimEnd('=') : dirHash;
            }

            var file = new FileInfo(path);
            string hash = $"{file.Name}{file.Length}{file.LastWriteTime.ToString("yyyyMMddHHmmss")}";
            return b64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(hash)).TrimEnd('=') : hash;
        }
    }
}
