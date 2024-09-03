using Flowframes.Data;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.Media;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using Force.Crc32;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.IO
{
    class IoUtils
    {
        public static Image GetImage(string path, bool allowMagickFallback = true, bool log = true)
        {
            var incompatibleExtensions = new List<string>() { "EXR" };

            try
            {
                string ext = new FileInfo(path).Extension.TrimStart('.').Upper();

                if (incompatibleExtensions.Contains(ext))
                    return null;

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    return Image.FromStream(stream);
            }
            catch
            {
                try
                {
                    MagickImage img = new MagickImage(path);
                    Bitmap bitmap = img.ToBitmap();

                    if (log)
                        Logger.Log($"GetImage: Native image reading for '{Path.GetFileName(path)}' failed - Using Magick.NET fallback instead.", true);

                    return bitmap;
                }
                catch (Exception e)
                {
                    if (log)
                        Logger.Log($"GetImage failed: {e.Message}", true);

                    return null;
                }
            }
        }

        public static string[] ReadLines(string path)
        {
            if (!File.Exists(path))
                return new string[0];

            List<string> lines = new List<string>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }

            return lines.ToArray();
        }

        public static bool IsPathDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            path = path.Trim();

            if (Directory.Exists(path))
                return true;

            if (File.Exists(path))
                return false;

            if (new string[2] { "\\", "/" }.Any((string x) => path.EndsWith(x)))
                return true;

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

        public static bool IsPathValid(string path)
        {
            if (path == null)
                return false;

            if (IsPathDirectory(path))
                return IsDirValid(path);
            else
                return IsFileValid(path);
        }

        public static void CopyDir(string sourceDirectory, string targetDirectory, bool move = false)
        {
            Directory.CreateDirectory(targetDirectory);
            DirectoryInfo source = new DirectoryInfo(sourceDirectory);
            DirectoryInfo target = new DirectoryInfo(targetDirectory);
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

        /// <summary>
        /// Async version of DeleteContentsOfDir, won't block main thread.
        /// </summary>
        public static async Task<bool> DeleteContentsOfDirAsync(string path)
        {
            ulong taskId = BackgroundTaskManager.Add($"DeleteContentsOfDirAsync {path}");
            bool returnVal = await Task.Run(async () => { return DeleteContentsOfDir(path); });
            BackgroundTaskManager.Remove(taskId);
            return returnVal;
        }

        /// <summary>
        /// Delete everything inside a directory except the dir itself.
        /// </summary>
        public static bool DeleteContentsOfDir(string path)
        {
            try
            {
                DeleteIfExists(path);
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                Logger.Log("DeleteContentsOfDir Error: " + e.Message, true);
                return false;
            }
        }

        public static void ReplaceInFilenamesDir(string dir, string textToFind, string textToReplace, bool recursive = true, string wildcard = "*")
        {
            int counter = 1;
            DirectoryInfo d = new DirectoryInfo(dir);
            FileInfo[] files;

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

        public static int GetAmountOfFiles(string path, bool recursive, string wildcard = "*")
        {
            try
            {
                if (!Directory.Exists(path))
                    return 0;

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

        static bool TryCopy(string source, string target, bool overwrite = true, bool showLog = false)
        {
            try
            {
                File.Copy(source, target, overwrite);
            }
            catch (Exception e)
            {
                if (showLog)
                    Logger.Log($"Failed to move '{source}' to '{target}' (Overwrite: {overwrite}): {e.Message}, !showLog");

                return false;
            }

            return true;
        }

        public static bool TryMove(string source, string target, bool overwrite = true, bool showLog = false)
        {
            try
            {
                if (overwrite && File.Exists(target))
                    File.Delete(target);

                File.Move(source, target);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to move '{source}' to '{target}' (Overwrite: {overwrite}): {e.Message}", !showLog);
                return false;
            }

            return true;
        }

        public static async Task RenameCounterDir(string path, int startAt = 0, int zPad = 8, bool inverse = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            int counter = startAt;
            DirectoryInfo d = new DirectoryInfo(path);
            FileInfo[] files = d.GetFiles();
            var filesSorted = files.OrderBy(n => n);

            if (inverse)
                filesSorted.Reverse();

            foreach (FileInfo file in files)
            {
                string dir = new DirectoryInfo(file.FullName).Parent.FullName;
                File.Move(file.FullName, Path.Combine(dir, counter.ToString().PadLeft(zPad, '0') + Path.GetExtension(file.FullName)));
                counter++;

                if (sw.ElapsedMilliseconds > 100)
                {
                    await Task.Delay(1);
                    sw.Restart();
                }
            }
        }

        public static async Task ReverseRenaming(string basePath, Dictionary<string, string> oldNewMap) // Relative -> absolute paths
        {
            Dictionary<string, string> absPaths = oldNewMap.ToDictionary(x => Path.Combine(basePath, x.Key), x => Path.Combine(basePath, x.Value));
            await ReverseRenaming(absPaths);
        }

        public static async Task ReverseRenaming(Dictionary<string, string> oldNewMap)  // Takes absolute paths only
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

        public static async Task<Fraction> GetVideoFramerate(string path)
        {
            string[] preferFfmpegReadoutFormats = new string[] { ".gif", ".png", ".apng", ".webp" };
            bool preferFfmpegReadout = preferFfmpegReadoutFormats.Contains(Path.GetExtension(path).Lower());

            Fraction fps = new Fraction();

            try
            {
                fps = await FfmpegCommands.GetFramerate(path, preferFfmpegReadout);
                Logger.Log("Detected FPS of " + Path.GetFileName(path) + " as " + fps + " FPS", true);
            }
            catch
            {
                Logger.Log("Failed to read FPS - Please enter it manually.");
            }

            return fps;
        }

        public static Fraction GetVideoFramerateForDir(string path)
        {
            Fraction fps = new Fraction();

            try
            {
                string parentDir = path.GetParentDir();
                string fpsFile = Path.Combine(parentDir, "fps.ini");
                fps = new Fraction(float.Parse(ReadLines(fpsFile)[0]));
                Logger.Log($"Got {fps} FPS from file: " + fpsFile);

                Fraction guiFps = Program.mainForm.GetCurrentSettings().inFps;

                DialogResult dialogResult = UiUtils.ShowMessageBox("A frame rate file has been found in the parent directory.\n\n" +
                    $"Click \"Yes\" to use frame rate from the file ({fps}) or \"No\" to use current FPS set in GUI ({guiFps})", "Load Frame Rate From fps.ini?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                    return fps;
                else if (dialogResult == DialogResult.No)
                    return guiFps;
            }
            catch { }
            return fps;
        }

        public static async Task<Size> GetVideoOrFramesRes(string path)
        {
            Size res = new Size();

            try
            {
                if (!IsPathDirectory(path))     // If path is video
                {
                    res = GetVideoRes(path);
                }
                else     // Path is frame folder
                {
                    Image thumb = await MainUiFunctions.GetThumbnail(path);
                    res = new Size(thumb.Width, thumb.Height);
                }
            }
            catch (Exception e)
            {
                Logger.Log("GetVideoOrFramesRes Error: " + e.Message);
            }

            return res;
        }

        public static Size GetVideoRes(string path)
        {
            Size size = new Size(0, 0);

            try
            {
                if (path.IsConcatFile())
                    path = ReadFileFirstLine(path).Split('\'')[1].Split('\'')[0];

                size = FfmpegCommands.GetSize(path);
                Logger.Log($"Detected video size of {Path.GetFileName(path)} as {size.Width}x{size.Height}", true);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to read video size!");
                Logger.Log(ex.ToString(), true);
            }

            return size;
        }

        /// <summary>
        /// Async (background thread) version of TryDeleteIfExists. Safe to run without awaiting.
        /// </summary>
        public static async Task<bool> TryDeleteIfExistsAsync(string path, int retries = 10)
        {
            string renamedPath = path;

            try
            {
                if (IsPathDirectory(path))
                {
                    while (Directory.Exists(renamedPath))
                        renamedPath += "_";

                    if (path != renamedPath)
                        Directory.Move(path, renamedPath);
                }
                else
                {
                    while (File.Exists(renamedPath))
                        renamedPath += "_";

                    if (path != renamedPath)
                        File.Move(path, renamedPath);
                }

                path = renamedPath;

                ulong taskId = BackgroundTaskManager.Add($"TryDeleteIfExistsAsync {path}");
                bool returnVal = await Task.Run(async () => { return TryDeleteIfExists(path); });
                BackgroundTaskManager.Remove(taskId);
                return returnVal;
            }
            catch (Exception e)
            {
                Logger.Log($"TryDeleteIfExistsAsync Move Exception: {e.Message} [{retries} retries left]", true);

                if (retries > 0)
                {
                    await Task.Delay(2000);
                    retries -= 1;
                    return await TryDeleteIfExistsAsync(path, retries);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Delete a path if it exists. Works for files and directories. Returns success status.
        /// </summary>
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

        public static bool DeleteIfExists(string path, bool log = false)		// Returns true if the file/dir exists
        {
            if (log)
                Logger.Log($"DeleteIfExists({path})", true);

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

        /// <summary> Delete file at <paramref name="path"/> if it's smaller than <paramref name="thresholdKb"/> KB. </summary>
        /// <returns> True if the path was valid and the file size was below the threshold, otherwise False. </returns>
        public static bool DeleteIfSmallerThanKb(string path, int thresholdKb = 4)
        {
            if (File.Exists(path) && (new FileInfo(path).Length / 1024f) < thresholdKb)
            {
                TryDeleteIfExists(path);
                return true;
            }
            else if (Directory.Exists(path) && GetDirSize(path, true) < thresholdKb)
            {
                TryDeleteIfExists(path);
                return true;
            }

            return false;
        }

        /// <summary> Add ".old" suffix to an existing file to avoid it getting overwritten. If one already exists, it will be ".old.old" etc. </summary>
        public static void RenameExistingFileOrDir(string path, int deleteSmallFileThresh = 2)
        {
            try
            {
                if (File.Exists(path))
                {
                    if (DeleteIfSmallerThanKb(path, deleteSmallFileThresh))
                        return;

                    string ext = Path.GetExtension(path);
                    string renamedPath = path;

                    while (File.Exists(renamedPath))
                        renamedPath = Path.ChangeExtension(renamedPath, null) + ".old" + ext;

                    File.Move(path, renamedPath);
                }
                else if (Directory.Exists(path))
                {
                    string renamedPath = path;

                    while (Directory.Exists(renamedPath))
                        renamedPath = renamedPath + ".old";

                    Directory.Move(path, renamedPath);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"RenameExistingFileOrDir: Failed to rename '{path}': {e.Message}", true);
            }
        }

        /// <summary>
        /// Add ".old" suffix to an existing folder to avoid it getting overwritten. If one already exists, it will be ".old.old" etc.
        /// </summary>
        public static void RenameExistingFolder(string path)
        {
            if (!Directory.Exists(path))
                return;

            try
            {
                string renamedPath = path;

                while (Directory.Exists(renamedPath))
                    renamedPath += ".old";

                Directory.Move(path, renamedPath);
            }
            catch (Exception e)
            {
                Logger.Log($"RenameExistingFolder: Failed to rename '{path}': {e.Message}", true);
            }
        }

        /// <summary>
        /// Easily rename a file without needing to specify the full move path
        /// </summary>
        public static bool RenameFile(string path, string newName, bool alsoRenameExtension = false)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                string ext = Path.GetExtension(path);
                string movePath = Path.Combine(dir, newName);

                if (!alsoRenameExtension)
                    movePath += ext;

                File.Move(path, movePath);
                return true;
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to rename '{path}' to '{newName}': {e.Message}", true);
                return false;
            }
        }

        public static async Task<string> GetCurrentExportFilename(bool fpsLimit, bool withExt)
        {
            InterpSettings curr = Interpolate.currentSettings;
            string max = Config.Get(Config.Key.maxFps);
            Fraction maxFps = max.Contains("/") ? new Fraction(max) : new Fraction(max.GetFloat());
            float fps = fpsLimit ? maxFps.GetFloat() : curr.outFps.GetFloat();

            Size outRes = await InterpolateUtils.GetOutputResolution(curr.inPath, true);
            string pattern = Config.Get(Config.Key.exportNamePattern);
            string inName = Interpolate.currentSettings.inputIsFrames ? Path.GetFileName(curr.inPath) : Path.GetFileNameWithoutExtension(curr.inPath);
            bool encodeBoth = Config.GetInt(Config.Key.maxFpsMode) == 0;
            bool addSuffix = fpsLimit && (!pattern.Contains("[FPS]") && !pattern.Contains("[ROUNDFPS]")) && encodeBoth;
            string filename = pattern;

            filename = filename.Replace("[NAME]", inName);
            filename = filename.Replace("[FULLNAME]", Path.GetFileName(curr.inPath));
            filename = filename.Replace("[FACTOR]", curr.interpFactor.ToStringDot());
            filename = filename.Replace("[AI]", curr.ai.NameShort.Upper());
            filename = filename.Replace("[MODEL]", curr.model.Name.Remove(" "));
            filename = filename.Replace("[FPS]", fps.ToStringDot("0.###"));
            filename = filename.Replace("[ROUNDFPS]", fps.RoundToInt().ToString());
            filename = filename.Replace("[RES]", $"{outRes.Width}x{outRes.Height}");
            filename = filename.Replace("[H]", $"{outRes.Height}p");

            if (addSuffix)
                filename += Paths.fpsLimitSuffix;

            if (withExt)
                filename += FfmpegUtils.GetExt(curr.outSettings);

            return filename;
        }

        public static string GetHighestFrameNumPath(string path)
        {
            FileInfo highest = null;
            int highestInt = -1;
            foreach (FileInfo frame in new DirectoryInfo(path).GetFiles("*.*", SearchOption.TopDirectoryOnly))
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

        public static string FilenameSuffix(string path, string suffix)
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

        public static async Task<Fraction> GetFpsFolderOrVideo(string path)
        {
            try
            {
                if (IsPathDirectory(path))
                {
                    Fraction dirFps = GetVideoFramerateForDir(path);

                    if (dirFps.GetFloat() > 0)
                        return dirFps;
                }
                else
                {
                    Fraction vidFps = await GetVideoFramerate(path);

                    if (vidFps.GetFloat() > 0)
                        return vidFps;
                }
            }
            catch (Exception e)
            {
                Logger.Log("GetFpsFolderOrVideo() Error: " + e.Message);
            }

            return new Fraction();
        }

        public enum ErrorMode { HiddenLog, VisibleLog, Messagebox }
        public static bool CanWriteToDir(string dir, ErrorMode errMode)
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
                    UiUtils.ShowMessageBox($"Can't write to {dir}!\n\n{e.Message}", UiUtils.MessageType.Error);
                return false;
            }
        }

        public static bool CopyTo(string file, string targetFolder, bool overwrite = true)
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

        public enum Hash { MD5, CRC32 }
        public static string GetHash(string path, Hash hashType, bool log = true)
        {
            string hashStr = "";
            NmkdStopwatch sw = new NmkdStopwatch();

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
                    hashStr = BitConverter.ToString(hash).Replace("-", "").Lower();
                }

                if (hashType == Hash.CRC32)
                {
                    var crc = new Crc32Algorithm();
                    var crc32bytes = crc.ComputeHash(stream);
                    hashStr = BitConverter.ToUInt32(crc32bytes, 0).ToString();
                }

                stream.Close();
            }
            catch (Exception e)
            {
                Logger.Log($"Error getting file hash for {Path.GetFileName(path)}: {e.Message}", true);
                return "";
            }

            if (log)
                Logger.Log($"Computed {hashType} for '{Path.GetFileNameWithoutExtension(path).Trunc(40) + Path.GetExtension(path)}' ({GetFilesizeStr(path)}): {hashStr} ({sw})", true);

            return hashStr;
        }

        public static async Task<string> GetHashAsync(string path, Hash hashType, bool log = true)
        {
            await Task.Delay(1);
            return GetHash(path, hashType, log);
        }

        public static bool CreateDir(string path)		// Returns whether the dir already existed
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

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
            if (exclude != null)
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
                    if (!noLog)
                        Logger.Log($"Failed to zero-pad {file} => {targetFilename}: {e.Message}", true);
                }
            }
        }

        public static bool CheckImageValid(string path)
        {
            try
            {
                Image img = GetImage(path);
                return (img.Width > 0 && img.Height > 0);
            }
            catch
            {
                return false;
            }
        }

        public static string[] GetFilesSorted(string path, bool recursive = false, string pattern = "*")
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
            if (!Directory.Exists(path))
                return new FileInfo[0];

            SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.GetFiles(pattern, opt).OrderBy(x => x.Name).ToArray();
        }

        public static bool CreateFileIfNotExists(string path)
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
                Logger.Log($"Failed to create file at '{path}': {e.Message}", true);
                return false;
            }
        }

        public static long GetDirSize(string path, bool recursive, string[] includedExtensions = null)
        {
            long size = 0;

            try
            {
                string[] files;
                StringComparison ignCase = StringComparison.OrdinalIgnoreCase;

                if (includedExtensions == null)
                    files = Directory.GetFiles(path);
                else
                    files = Directory.GetFiles(path).Where(file => includedExtensions.Any(x => file.EndsWith(x, ignCase))).ToArray();

                foreach (string file in files)
                {
                    try { size += new FileInfo(file).Length; } catch { size += 0; }
                }

                if (!recursive)
                    return size;

                // Add subdirectory sizes.
                DirectoryInfo[] dis = new DirectoryInfo(path).GetDirectories();
                foreach (DirectoryInfo di in dis)
                    size += GetDirSize(di.FullName, true, includedExtensions);
            }
            catch (Exception e)
            {
                Logger.Log($"GetDirSize Error: {e.Message}\n{e.StackTrace}", true);
            }

            return size;
        }

        public static long GetPathSize(string path)
        {
            try
            {
                bool isFile = File.Exists(path);
                return isFile ? GetFilesize(path) : GetDirSize(path, true);
            }
            catch
            {
                return -1;
            }
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

        public static string GetFilesizeStr(string path)
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

        public static void OverwriteFileWithText(string path, string text = "THIS IS A DUMMY FILE - DO NOT DELETE ME")
        {
            try
            {
                File.WriteAllText(path, text);
            }
            catch (Exception e)
            {
                Logger.Log($"OverwriteWithText failed for '{path}': {e.Message}", true);
            }
        }

        public static long GetDiskSpace(string path, bool mbytes = true)
        {
            try
            {
                string driveLetter = path.Substring(0, 2);      // Make 'C:/some/random/path' => 'C:' etc
                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady && d.Name.StartsWith(driveLetter))
                    {
                        if (mbytes)
                            return (long)(d.AvailableFreeSpace / 1024f / 1000f);
                        else
                            return d.AvailableFreeSpace;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error trying to get disk space: " + e.Message, true);
            }

            return 0;
        }

        public static string[] GetUniqueExtensions(string path, bool recursive = false)
        {
            FileInfo[] fileInfos = GetFileInfosSorted(path, recursive);
            List<string> exts = fileInfos.Select(x => x.Extension).ToList();
            return exts.Select(x => x).Distinct().ToArray();
        }

        public static string ReadFile(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static string[] ReadFileLines(string path)
        {
            return ReadFile(path).SplitIntoLines();
        }

        public static string ReadFileFirstLine(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    return streamReader.ReadLine();
                }
            }
        }
    }
}
