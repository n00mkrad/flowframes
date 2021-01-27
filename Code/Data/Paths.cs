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
		public const string scenesDir = "scenes";
		public const string alphaSuffix = "-a";

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
