
using Flowframes.Data;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.UI;
using Force.Crc32;
using Microsoft.WindowsAPICodePack.Shell;
using Standart.Hash.xxHash;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.IO
{
    class IOUtils
    {
		public static string GetExe()
		{
			return System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase.Replace("file:///", "");
		}

		public static string GetExeDir()
		{
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public static Image GetImage(string path)
		{
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
				return Image.FromStream(stream);
		}

		public static string[] ReadLines(string path)
		{
			List<string> lines = new List<string>();
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
			using (var sr = new StreamReader(fs, Encoding.UTF8))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
					lines.Add(line);
			}
			return lines.ToArray();
		}

		public static bool IsPathDirectory(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			path = path.Trim();
			if (Directory.Exists(path))
			{
				return true;
			}
			if (File.Exists(path))
			{
				return false;
			}
			if (new string[2]
			{
				"\\",
				"/"
			}.Any((string x) => path.EndsWith(x)))
			{
				return true;
			}
			return string.IsNullOrWhiteSpace(Path.GetExtension(path));
		}

		public static bool IsFileValid(string path)
		{
			if (path == null)
				return false;

			if (!File.Exists(path))
				return false;

			return true;
		}

		public static bool IsDirValid(string path)
		{
			if (path == null)
				return false;

			if (!Directory.Exists(path))
				return false;

			return true;
		}

		public static void Copy(string sourceDirectoryName, string targetDirectoryName, bool move = false)
		{
			Directory.CreateDirectory(targetDirectoryName);
			DirectoryInfo source = new DirectoryInfo(sourceDirectoryName);
			DirectoryInfo target = new DirectoryInfo(targetDirectoryName);
			CopyWork(source, target, move);
		}

		private static void CopyWork(DirectoryInfo source, DirectoryInfo target, bool move)
		{
			DirectoryInfo[] directories = source.GetDirectories();
			foreach (DirectoryInfo directoryInfo in directories)
			{
				CopyWork(directoryInfo, target.CreateSubdirectory(directoryInfo.Name), move);
			}
			FileInfo[] files = source.GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				if (move)
					fileInfo.MoveTo(Path.Combine(target.FullName, fileInfo.Name));
				else
					fileInfo.CopyTo(Path.Combine(target.FullName, fileInfo.Name), overwrite: true);
			}
		}

		public static void DeleteContentsOfDir(string path)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			FileInfo[] files = directoryInfo.GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				fileInfo.Delete();
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			foreach (DirectoryInfo directoryInfo2 in directories)
			{
				directoryInfo2.Delete(recursive: true);
			}
		}

		public static void ReplaceInFilenamesDir(string dir, string textToFind, string textToReplace, bool recursive = true, string wildcard = "*")
		{
			int counter = 1;
			DirectoryInfo d = new DirectoryInfo(dir);
			FileInfo[] files = null;
			if (recursive)
				files = d.GetFiles(wildcard, SearchOption.AllDirectories);
			else
				files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);
			foreach (FileInfo file in files)
			{
				ReplaceInFilename(file.FullName, textToFind, textToReplace);
				counter++;
			}
		}

		public static void ReplaceInFilename(string path, string textToFind, string textToReplace)
		{
			string ext = Path.GetExtension(path);
			string newFilename = Path.GetFileNameWithoutExtension(path).Replace(textToFind, textToReplace);
			string targetPath = Path.Combine(Path.GetDirectoryName(path), newFilename + ext);
			if (File.Exists(targetPath))
			{
				//Program.Print("Skipped " + path + " because a file with the target name already exists.");
				return;
			}
			File.Move(path, targetPath);
		}

		public static int GetFilenameCounterLength(string file, string prefixToRemove = "")
		{
			string filenameNoExt = Path.GetFileNameWithoutExtension(file);
			if (!string.IsNullOrEmpty(prefixToRemove))
				filenameNoExt = filenameNoExt.Replace(prefixToRemove, "");
			string onlyNumbersFilename = Regex.Replace(filenameNoExt, "[^.0-9]", "");
			return onlyNumbersFilename.Length;
		}

		public static int GetAmountOfFiles (string path, bool recursive, string wildcard = "*")
        {
            try
            {
				DirectoryInfo d = new DirectoryInfo(path);
				FileInfo[] files = null;
				if (recursive)
					files = d.GetFiles(wildcard, SearchOption.AllDirectories);
				else
					files = d.GetFiles(wildcard, SearchOption.TopDirectoryOnly);
				return files.Length;
			}
			catch
            {
				return 0;
            }
		}

		static bool TryCopy(string source, string target, bool overwrite = true)
		{
			try
			{
				File.Copy(source, target, overwrite);
			}
			catch (Exception e)
			{
				Logger.Log($"Failed to move '{source}' to '{target}' (Overwrite: {overwrite}): {e.Message}");
				return false;
			}

			return true;
		}

		public static bool TryMove(string source, string target, bool overwrite = true)
		{
			try
			{
				if (overwrite && File.Exists(target))
					File.Delete(target);

				File.Move(source, target);
			}
			catch (Exception e)
			{
				Logger.Log($"Failed to move '{source}' to '{target}' (Overwrite: {overwrite}): {e.Message}");
				return false;
			}

			return true;
		}

		public static void RenameCounterDir(string path, bool inverse = false)
		{
			int counter = 1;
			DirectoryInfo d = new DirectoryInfo(path);
			FileInfo[] files = d.GetFiles();
			var filesSorted = files.OrderBy(n => n);
			if (inverse)
				filesSorted.Reverse();
			foreach (FileInfo file in files)
			{
				string dir = new DirectoryInfo(file.FullName).Parent.FullName;
				int filesDigits = (int)Math.Floor(Math.Log10((double)files.Length) + 1);
				File.Move(file.FullName, Path.Combine(dir, counter.ToString().PadLeft(filesDigits, '0') + Path.GetExtension(file.FullName)));
				counter++;
				//if (counter % 100 == 0) Program.Print("Renamed " + counter + " files...");
			}
		}

		public static async Task<Dictionary<string, string>> RenameCounterDirReversibleAsync(string path, string ext, int startAt, int padding = 0)
		{
			Stopwatch sw = new Stopwatch();
			sw.Restart();
			Dictionary<string, string> oldNewNamesMap = new Dictionary<string, string>();

			int counter = startAt;
			FileInfo[] files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly);
			var filesSorted = files.OrderBy(n => n);

			foreach (FileInfo file in files)
			{
				string dir = new DirectoryInfo(file.FullName).Parent.FullName;
				int filesDigits = (int)Math.Floor(Math.Log10((double)files.Length) + 1);
				string newFilename = (padding > 0) ? counter.ToString().PadLeft(padding, '0') : counter.ToString();
				string outpath = outpath = Path.Combine(dir, newFilename + Path.GetExtension(file.FullName));
				File.Move(file.FullName, outpath);
				oldNewNamesMap.Add(file.FullName, outpath);
				counter++;

				if(sw.ElapsedMilliseconds > 100)
                {
					await Task.Delay(1);
					sw.Restart();
                }
			}

			return oldNewNamesMap;
		}

		public static async Task ReverseRenaming(string basePath, Dictionary<string, string> oldNewMap)	// Relative -> absolute paths
		{
			Dictionary<string, string> absPaths = oldNewMap.ToDictionary(x => Path.Combine(basePath, x.Key), x => Path.Combine(basePath, x.Value));
			await ReverseRenaming(absPaths);
		}

		public static async Task ReverseRenaming(Dictionary<string, string> oldNewMap)	// Takes absolute paths only
		{
			if (oldNewMap == null || oldNewMap.Count < 1) return;
			int counter = 0;
			int failCount = 0;

			foreach (KeyValuePair<string, string> pair in oldNewMap)
            {
				bool success = TryMove(pair.Value, pair.Key);

				if (!success)
					failCount++;

				if (failCount >= 100)
					break;

				counter++;

				if (counter % 1000 == 0)
					await Task.Delay(1);
			}
		}

		public static async Task<float> GetVideoFramerate (string path)
        {
			float fps = 0;
            try
            {
				ShellFile shellFile = ShellFile.FromFilePath(path);
				fps = (float)shellFile.Properties.System.Video.FrameRate.Value / 1000f;
				Logger.Log("Detected FPS of " + Path.GetFileName(path) + " as " + fps + " FPS", true);
				if (fps <= 0)
					throw new Exception("FPS is 0.");
			}
			catch
            {
				Logger.Log("Failed to read FPS - Trying alternative method...", true);
				try
				{
					fps = await FfmpegCommands.GetFramerate(path);
					Logger.Log("Detected FPS of " + Path.GetFileName(path) + " as " + fps + " FPS", true);
				}
				catch
                {
					Logger.Log("Failed to read FPS - Please enter it manually.");
				}
			}
			return fps;
		}

		public static float GetVideoFramerateForDir(string path)
		{
			float fps = 0;
            try
            {
				string parentDir = path.GetParentDir();
				string fpsFile = Path.Combine(parentDir, "fps.ini");
				fps = float.Parse(ReadLines(fpsFile)[0]);
				Logger.Log($"Got {fps} FPS from file: " + fpsFile);

				float guiFps = Program.mainForm.GetCurrentSettings().inFps;

				DialogResult dialogResult = MessageBox.Show("A frame rate file has been found in the parent directory.\n\n" +
					$"Click \"Yes\" to use frame rate from the file ({fps}) or \"No\" to use current FPS set in GUI ({guiFps})", "Load Frame Rate From fps.ini?", MessageBoxButtons.YesNo);
				if (dialogResult == DialogResult.Yes)
					return fps;
				else if (dialogResult == DialogResult.No)
					return guiFps;
			}
			catch { }
			return fps;
		}

		public static async Task<Size> GetVideoOrFramesRes (string path)
        {
			Size res = new Size();
			if (!IsPathDirectory(path))     // If path is video
			{
				res = GetVideoRes(path);
			}
			else     // Path is frame folder
			{
				Image thumb = await MainUiFunctions.GetThumbnail(path);
				res = new Size(thumb.Width, thumb.Height);
			}
			return res;
		}

		public static Size GetVideoRes (string path)
		{
			Size size = new Size(0, 0);
			try
			{
				ShellFile shellFile = ShellFile.FromFilePath(path);
				int w = (int)shellFile.Properties.System.Video.FrameWidth.Value;
				int h = (int)shellFile.Properties.System.Video.FrameHeight.Value;
				return new Size(w, h);
			}
			catch (Exception e)
			{
				Logger.Log($"Failed to read video size ({e.Message}) - Trying alternative method...", true);
				try
				{
					size = FfmpegCommands.GetSize(path);
					Logger.Log($"Detected video size of {Path.GetFileName(path)} as {size.Width}x{size.Height}", true);
				}
				catch
				{
					Logger.Log("Failed to read video size!");
				}
			}
			return size;
		}

		public static bool TryDeleteIfExists(string path)      // Returns true if no exception occurs
		{
            try
            {
				if (path == null)
					return false;
				DeleteIfExists(path);
				return true;
			}
			catch (Exception e)
            {
				Logger.Log($"TryDeleteIfExists: Error trying to delete {path}: {e.Message}", true);
				return false;
            }
		}

		public static bool DeleteIfExists (string path)		// Returns true if the file/dir exists
        {
            if (!IsPathDirectory(path) && File.Exists(path))
            {
				File.Delete(path);
				return true;
            }
			if (IsPathDirectory(path) && Directory.Exists(path))
			{
				Directory.Delete(path, true);
				return true;
			}
			return false;
        }

		public static string GetCurrentExportSuffix ()
        {
			return GetExportSuffix(Interpolate.current.interpFactor, Interpolate.current.ai, Interpolate.current.model);
		}

		public static string GetExportSuffix(float factor, AI ai, string mdl)
		{
			string suffix = $"-{factor.ToStringDot()}x-{ai.aiNameShort.ToUpper()}";
			if (Config.GetBool("modelSuffix"))
				suffix += $"-{mdl}";
			return suffix;
		}

		public static string GetHighestFrameNumPath (string path)
        {
			FileInfo highest = null;
			int highestInt = -1;
			foreach(FileInfo frame in new DirectoryInfo(path).GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
				int num = frame.Name.GetInt();
				if (num > highestInt)
                {
					highest = frame;
					highestInt = frame.Name.GetInt();
				}
            }
			return highest.FullName;
        }

		public static string FilenameSuffix (string path, string suffix)
        {
            try
            {
				string ext = Path.GetExtension(path);
				return Path.Combine(path.GetParentDir(), $"{Path.GetFileNameWithoutExtension(path)}{suffix}{ext}");
			}
            catch
            {
				return path;
            }
		}

		public static string GetAudioFile (string basePath)
        {
			string[] exts = new string[] { "m4a", "wav", "ogg", "mp2", "mp3" };

			foreach(string ext in exts)
            {
				string filename = Path.ChangeExtension(basePath, ext);
				if (File.Exists(filename))
					return filename;
			}

			return null;
		}

		public static async Task<float> GetFpsFolderOrVideo(string path)
		{
            try
            {
				if (IsPathDirectory(path))
				{
					float dirFps = GetVideoFramerateForDir(path);

					if (dirFps > 0)
						return dirFps;
				}
				else
				{
					float vidFps = await GetVideoFramerate(path);

					if (vidFps > 0)
						return vidFps;
				}
			}
			catch (Exception e)
            {
				Logger.Log("GetFpsFolderOrVideo() Error: " + e.Message);
            }

			return 0;
		}

		public enum ErrorMode { HiddenLog, VisibleLog, Messagebox }
		public static bool CanWriteToDir (string dir, ErrorMode errMode)
        {
			string tempFile = Path.Combine(dir, "flowframes-testfile.tmp");
            try
            {
				File.Create(tempFile);
				File.Delete(tempFile);
				return true;
			}
			catch (Exception e)
            {
				Logger.Log($"Can't write to {dir}! {e.Message}", errMode == ErrorMode.HiddenLog);
				if (errMode == ErrorMode.Messagebox && !BatchProcessing.busy)
					MessageBox.Show($"Can't write to {dir}!\n\n{e.Message}", "Error");
				return false;
            }
        }

		public static bool CopyTo (string file, string targetFolder, bool overwrite = true)
        {
			string targetPath = Path.Combine(targetFolder, Path.GetFileName(file));
            try
            {
				if (!Directory.Exists(targetFolder))
					Directory.CreateDirectory(targetFolder);
				File.Copy(file, targetPath, overwrite);
				return true;
            }
			catch (Exception e)
            {
				Logger.Log($"Failed to copy {file} to {targetFolder}: {e.Message}");
				return false;
            }
        }

		public static bool MoveTo(string file, string targetFolder, bool overwrite = true)
		{
			string targetPath = Path.Combine(targetFolder, Path.GetFileName(file));
			try
			{
				if (!Directory.Exists(targetFolder))
					Directory.CreateDirectory(targetFolder);
				if (overwrite)
					DeleteIfExists(targetPath);
				File.Move(file, targetPath);
				return true;
			}
			catch (Exception e)
			{
				Logger.Log($"Failed to move {file} to {targetFolder}: {e.Message}");
				return false;
			}
		}

		public enum Hash { MD5, CRC32, xxHash }
		public static string GetHash (string path, Hash hashType, bool log = true, bool quick = true)
		{
			Benchmarker.Start();
			string hashStr = "";
            if (IsPathDirectory(path))
            {
				Logger.Log($"Path '{path}' is directory! Returning empty hash.", true);
				return hashStr;
            }
            try
            {
				var stream = File.OpenRead(path);

				if (hashType == Hash.MD5)
				{
					MD5 md5 = MD5.Create();
					var hash = md5.ComputeHash(stream);
					hashStr = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}

				if (hashType == Hash.CRC32)
				{
					var crc = new Crc32Algorithm();
					var crc32bytes = crc.ComputeHash(stream);
					hashStr = BitConverter.ToUInt32(crc32bytes, 0).ToString();
				}

				if (hashType == Hash.xxHash)
				{
					ulong xxh64 = xxHash64.ComputeHash(stream, 8192, (ulong)GetFilesize(path));
					hashStr = xxh64.ToString();
				}

				stream.Close();
			}
			catch (Exception e)
            {
				Logger.Log($"Error getting file hash for {Path.GetFileName(path)}: {e.Message}", true);
				return "";
            }
			if (log)
				Logger.Log($"Computed {hashType} for '{Path.GetFileNameWithoutExtension(path).Trunc(40) + Path.GetExtension(path)}' ({GetFilesizeStr(path)}) in {Benchmarker.GetTimeStr(true)}: {hashStr}", true);
			return hashStr;
		}

		public static async Task<string> GetHashAsync(string path, Hash hashType, bool log = true, bool quick = true)
		{
			await Task.Delay(1);
			return GetHash(path, hashType, log, quick);
		}

		public static bool CreateDir (string path)		// Returns whether the dir already existed
        {
			if (!Directory.Exists(path))
            {
				Directory.CreateDirectory(path);
				return false;
			}
			return true;
        }

		public static void ZeroPadDir(string path, string ext, int targetLength, bool recursive = false)
		{
			FileInfo[] files;
			if (recursive)
				files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.AllDirectories);
			else
				files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly);

			ZeroPadDir(files.Select(x => x.FullName).ToList(), targetLength);
		}

		public static void ZeroPadDir(List<string> files, int targetLength, List<string> exclude = null, bool noLog = true)
		{
			if(exclude != null)
				files = files.Except(exclude).ToList();

			foreach (string file in files)
			{
				string fname = Path.GetFileNameWithoutExtension(file);
				string targetFilename = Path.Combine(Path.GetDirectoryName(file), fname.PadLeft(targetLength, '0') + Path.GetExtension(file));
                try
                {
					if (targetFilename != file)
						File.Move(file, targetFilename);
				}
				catch (Exception e)
				{
					if(!noLog)
						Logger.Log($"Failed to zero-pad {file} => {targetFilename}: {e.Message}", true);
				}
			}
		}

		public static bool CheckImageValid (string path)
        {
            try
            {
				Image img = GetImage(path);
				if (img.Width > 1 && img.Height > 1)
					return true;
				return false;
            }
			catch
            {
				return false;
            }
        }

		public static string[] GetFilesSorted (string path, bool recursive = false, string pattern = "*")
        {
			SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			return Directory.GetFiles(path, pattern, opt).OrderBy(x => Path.GetFileName(x)).ToArray();
        }

		public static string[] GetFilesSorted(string path, string pattern = "*")
		{
			return GetFilesSorted(path, false, pattern);
		}

		public static string[] GetFilesSorted(string path)
		{
			return GetFilesSorted(path, false, "*");
		}

		public static FileInfo[] GetFileInfosSorted(string path, bool recursive = false, string pattern = "*")
		{
			SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			DirectoryInfo dir = new DirectoryInfo(path);
			return dir.GetFiles(pattern, opt).OrderBy(x => x.Name).ToArray();
		}

		public static bool CreateFileIfNotExists (string path)
        {
			if (File.Exists(path))
				return false;

            try
            {
				File.Create(path).Close();
				return true;
            }
			catch (Exception e)
            {
				Logger.Log($"Failed to create file at '{path}': {e.Message}");
				return false;
            }
        }

		public static long GetDirSize(string path, bool recursive, string[] includedExtensions = null)
		{
			long size = 0;
			// Add file sizes.
			string[] files;
			StringComparison ignCase = StringComparison.OrdinalIgnoreCase;
			if (includedExtensions == null)
				files = Directory.GetFiles(path);
			else
				files = Directory.GetFiles(path).Where(file => includedExtensions.Any(x => file.EndsWith(x, ignCase))).ToArray();

			foreach (string file in files)
				size += new FileInfo(file).Length;

			if (!recursive)
				return size;

			// Add subdirectory sizes.
			DirectoryInfo[] dis = new DirectoryInfo(path).GetDirectories();
			foreach (DirectoryInfo di in dis)
				size += GetDirSize(di.FullName, true, includedExtensions);

			return size;
		}

		public static long GetFilesize(string path)
		{
            try
            {
				return new FileInfo(path).Length;
			}
            catch
            {
				return -1;
            }
		}

		public static string GetFilesizeStr (string path)
        {
			try
			{
				return FormatUtils.Bytes(GetFilesize(path));
			}
			catch
			{
				return "?";
			}
		}

		public static byte[] GetLastBytes (string path, int startAt, int bytesAmount)
        {
			byte[] buffer = new byte[bytesAmount];
			using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
			{
				reader.BaseStream.Seek(startAt, SeekOrigin.Begin);
				reader.Read(buffer, 0, bytesAmount);
			}
			return buffer;
		}

		public static bool HasBadChars(string str)
		{
			return str != str.StripBadChars();
		}

		public static void OverwriteFileWithText (string path, string text = "THIS IS A DUMMY FILE - DO NOT DELETE ME")
        {
            try
            {
				File.WriteAllText(path, text);
			}
			catch (Exception e)
            {
				Logger.Log($"OverwriteWithText failed for '{path}': {e.Message}");
            }
		}
	}
}
