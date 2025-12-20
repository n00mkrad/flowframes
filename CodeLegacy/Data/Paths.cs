using System;
using System.IO;

namespace Flowframes.IO
{
    class Paths
    {
        public const string framesDir = "frames";
        public const string interpDir = "interp";
        public const string chunksDir = "vchunks";
        public const string resumeDir = "resumedata";
        public const string scenesDir = "scenes";

        public const string symlinksSuffix = "-symlinks";
        public const string alphaSuffix = "-a";
        public const string prevSuffix = "-previous";
        public const string fpsLimitSuffix = "-fpsLimit";
        public const string backupSuffix = ".bak";

        public const string frameOrderPrefix = "frames";

        public const string audioSuffix = "audio";

        public const string audioVideoDir = "av";
        public const string licensesDir = "licenses";

        private static string _sessionTimestamp;
        public static string SessionTimestamp { get => _sessionTimestamp.IsNotEmpty() ? _sessionTimestamp : Init(); }

        public static string Init()
        {
            if (_sessionTimestamp.IsNotEmpty())
                return _sessionTimestamp;

            var n = DateTime.Now;
            _sessionTimestamp = $"{n.Year}-{n.Month}-{n.Day}-{n.Hour}-{n.Minute}-{n.Second}-{n.Millisecond}";
            return _sessionTimestamp;
        }

        public static string GetFrameOrderFilename(float factor)
        {
            return $"{frameOrderPrefix}-{factor.ToString()}x.ini";
        }

        public static string GetFrameOrderFilenameChunk(int from, int to)
        {
            return $"{frameOrderPrefix}-chunk-{from}-{to}.ini";
        }

        public static string GetExe()
        {
            return System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase.Replace("file:///", "");
        }

        public static string GetExeDir()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetVerPath()
        {
            return Path.Combine(GetDataPath(), "ver.ini");
        }

        public static string GetDataPath()
        {
            string path = Path.Combine(GetExeDir(), "FlowframesData");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetSessionsPath()
        {
            string path = Path.Combine(GetDataPath(), "sessions");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetPkgPath()
        {
            string path = Path.Combine(GetDataPath(), "pkgs");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetLogPath(bool noSession = false)
        {
            string path = Path.Combine(GetDataPath(), "logs", (noSession ? "" : SessionTimestamp));
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetCachePath()
        {
            string path = Path.Combine(GetDataPath(), "cache");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetSessionDataPath()
        {
            string path = Path.Combine(GetSessionsPath(), SessionTimestamp);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetFrameSeqPath(bool noSession = false)
        {
            string path = Path.Combine((noSession ? GetDataPath() : GetSessionDataPath()), "frameSequences");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
