
using Flowframes.Data;
using Flowframes.Main;
using Force.Crc32;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
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

		public static bool TryCopy(string source, string dest, bool overwrite = true)      // Copy with error handling. Returns false if failed
		{
			try
			{
				File.Copy(source, dest, overwrite);
			}
			catch (Exception e)
			{
				MessageBox.Show("Copy from \"" + source + "\" to \"" + dest + " (Overwrite: " + overwrite + ") failed: \n\n" + e.Message);
				return false;
			}
			return true;
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

		static bool TryCopy(string source, string target)
		{
			try
			{
				File.Copy(source, target);
			}
			catch
			{
				return false;
			}
			return true;
		}

		static bool TryMove(string source, string target, bool deleteIfExists = true)
		{
			try
			{
				if (deleteIfExists && File.Exists(target))
					File.Delete(target);
				File.Move(source, target);
			}
			catch
			{
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

		public static Dictionary<string, string> RenameCounterDirReversible(string path, string ext, int startAt, int padding = 0)
		{
			Dictionary<string, string> oldNewNamesMap = new Dictionary<string, string>();

			int counter = startAt;
			FileInfo[] files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly);
			var filesSorted = files.OrderBy(n => n);

			foreach (FileInfo file in files)
			{
				string dir = new DirectoryInfo(file.FullName).Parent.FullName;
				int filesDigits = (int)Math.Floor(Math.Log10((double)files.Length) + 1);
				string outpath = "";
				if (padding > 0)
					outpath = Path.Combine(dir, counter.ToString().PadLeft(padding, '0') + Path.GetExtension(file.FullName));
				else
					outpath = Path.Combine(dir, counter.ToString() + Path.GetExtension(file.FullName));
				File.Move(file.FullName, outpath);
				oldNewNamesMap.Add(file.FullName, outpath);
				counter++;
			}

			return oldNewNamesMap;
		}

		public static void ReverseRenaming(Dictionary<string, string> oldNewMap, bool clearDict)
		{
			if (oldNewMap == null || oldNewMap.Count < 1) return;
			foreach (KeyValuePair<string, string> pair in oldNewMap)
				TryMove(pair.Value, pair.Key);
			if (clearDict)
				oldNewMap.Clear();
		}

		public static float GetVideoFramerate (string path)
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
					fps = FFmpegCommands.GetFramerate(path);
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
					size = FFmpegCommands.GetSize(path);
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

		public static string GetAiSuffix (AI ai, int times)
        {
			return $"-{times}x-{ai.aiNameShort.ToUpper()}";
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

		public static float GetFpsFolderOrVideo(string path)
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
					float vidFps = GetVideoFramerate(path);
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

		public enum Hash { MD5, CRC32 }
		public static string GetHash (string filename, Hash hashType)
		{
			if (hashType == Hash.MD5)
            {
				MD5 md5 = MD5.Create();
				var hash = md5.ComputeHash(File.OpenRead(filename));
				return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			}
			if (hashType == Hash.CRC32)
			{
				var crc = new Crc32Algorithm();
				var crc32bytes = crc.ComputeHash(File.OpenRead(filename));
				return BitConverter.ToUInt32(crc32bytes, 0).ToString();
			}
			return null;
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

		public static void ZeroPadDir(List<string> files, int targetLength, List<string> exclude = null)
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
	}
}
