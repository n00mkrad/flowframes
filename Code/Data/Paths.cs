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

		public const string alphaSuffix = "-a";
		public const string prevSuffix = "-previous";

		public const string frameOrderPrefix = "frames";

		public static string GetFrameOrderFilename(float factor)
		{
			return $"{frameOrderPrefix}-{factor.ToStringDot()}x.ini";
		}

		public static string GetFrameOrderFilenameChunk (int from, int to)
		{
			return $"{frameOrderPrefix}-chunk-{from}-{to}.ini";
		}

		public static string GetVerPath()
		{
			return Path.Combine(GetDataPath(), "ver.ini");
		}

		public static string GetDataPath ()
		{
			string path = Path.Combine(IOUtils.GetExeDir(), "FlowframesData");
			Directory.CreateDirectory(path);
			return path;
		}


		public static string GetPkgPath()
		{
			string path = Path.Combine(GetDataPath(), "pkgs");
			Directory.CreateDirectory(path);
			return path;
		}

		public static string GetLogPath()
		{
			string path = Path.Combine(GetDataPath(), "logs");
			Directory.CreateDirectory(path);
			return path;
		}
	}
}
