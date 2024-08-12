using System;
using System.Collections.Generic;
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

		public static string sessionTimestamp;

		public static void Init()
		{
			var n = DateTime.Now;
			sessionTimestamp = $"{n.Year}-{n.Month}-{n.Day}-{n.Hour}-{n.Minute}-{n.Second}-{n.Millisecond}";
		}

		public static string GetFrameOrderFilename(float factor)
		{
			return $"{frameOrderPrefix}-{factor.ToStringDot()}x.ini";
		}

		public static string GetFrameOrderFilenameChunk (int from, int to)
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

		public static string GetDataPath ()
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
			string path = Path.Combine(GetDataPath(), "logs", (noSession ? "" : sessionTimestamp));
			Directory.CreateDirectory(path);
			return path;
		}

		public static string GetSessionDataPath()
		{
			string path = Path.Combine(GetSessionsPath(), sessionTimestamp);
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
