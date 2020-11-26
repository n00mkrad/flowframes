using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.IO
{
    class Formats
    {
        public static string[] supported = { ".mp4", ".m4v", ".gif", ".mkv", ".mpg", ".webm", ".avi", ".wmv", ".ts", ".bik" };     // Supported formats
        public static string[] noEncodeSupport = { ".bik" };    // Files that have no encode support, but decode
        public static string[] preprocess = { ".gif" };     // Files that get converted to MP4 first for compat reasons
    }
}
