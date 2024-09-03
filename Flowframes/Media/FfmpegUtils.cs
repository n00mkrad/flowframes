using Flowframes.Data;
using Flowframes.Data.Streams;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Os;
using Flowframes.Properties;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.Data.Enums.Encoding;
using static Flowframes.Media.GetVideoInfo;
using Stream = Flowframes.Data.Streams.Stream;
using static NmkdUtils.StringExtensions;

namespace Flowframes.Media
{
    class FfmpegUtils
    {
        private readonly static FfprobeMode showStreams = FfprobeMode.ShowStreams;
        private readonly static FfprobeMode showFormat = FfprobeMode.ShowFormat;

        public static List<Encoder> CompatibleHwEncoders = new List<Encoder>();
        public static bool NvencSupportsBFrames = false;

        public static async Task<int> GetStreamCount(string path)
        {
            Logger.Log($"GetStreamCount({path})", true);
            string output = await GetFfmpegInfoAsync(path, "Stream #0:");

            if (string.IsNullOrWhiteSpace(output.Trim()))
                return 0;

            return output.SplitIntoLines().Where(x => x.MatchesWildcard("*Stream #0:*: *: *")).Count();
        }

        public static async Task<List<Stream>> GetStreams(string path, bool progressBar, int streamCount, Fraction? defaultFps, bool countFrames)
        {
            List<Stream> streamList = new List<Stream>();

            try
            {
                if (defaultFps == null)
                    defaultFps = new Fraction(30, 1);

                string output = await GetFfmpegInfoAsync(path, "Stream #0:");
                string[] streams = output.SplitIntoLines().Where(x => x.MatchesWildcard("*Stream #0:*: *: *")).ToArray();

                foreach (string streamStr in streams)
                {
                    try
                    {
                        int idx = streamStr.Split(':')[1].Split('[')[0].Split('(')[0].GetInt();
                        bool def = await GetFfprobeInfoAsync(path, showStreams, "DISPOSITION:default", idx) == "1";

                        if (progressBar)
                            Program.mainForm.SetProgress(FormatUtils.RatioInt(idx + 1, streamCount));

                        if (streamStr.Contains(": Video:"))
                        {
                            string lang = await GetFfprobeInfoAsync(path, showStreams, "TAG:language", idx);
                            string title = await GetFfprobeInfoAsync(path, showStreams, "TAG:title", idx);
                            string codec = await GetFfprobeInfoAsync(path, showStreams, "codec_name", idx);
                            string codecLong = await GetFfprobeInfoAsync(path, showStreams, "codec_long_name", idx);
                            string pixFmt = (await GetFfprobeInfoAsync(path, showStreams, "pix_fmt", idx)).Upper();
                            int kbits = (await GetFfprobeInfoAsync(path, showStreams, "bit_rate", idx)).GetInt() / 1024;
                            Size res = await GetMediaResolutionCached.GetSizeAsync(path);
                            Size sar = SizeFromString(await GetFfprobeInfoAsync(path, showStreams, "sample_aspect_ratio", idx));
                            Size dar = SizeFromString(await GetFfprobeInfoAsync(path, showStreams, "display_aspect_ratio", idx));
                            int frameCount = countFrames ? await GetFrameCountCached.GetFrameCountAsync(path) : 0;
                            FpsInfo fps = await GetFps(path, streamStr, idx, (Fraction)defaultFps, frameCount);
                            VideoStream vStream = new VideoStream(lang, title, codec, codecLong, pixFmt, kbits, res, sar, dar, fps, frameCount);
                            vStream.Index = idx;
                            vStream.IsDefault = def;
                            Logger.Log($"Added video stream: {vStream}", true);
                            streamList.Add(vStream);
                            continue;
                        }

                        if (streamStr.Contains(": Audio:"))
                        {
                            string lang = await GetFfprobeInfoAsync(path, showStreams, "TAG:language", idx);
                            string title = await GetFfprobeInfoAsync(path, showStreams, "TAG:title", idx);
                            string codec = await GetFfprobeInfoAsync(path, showStreams, "codec_name", idx);
                            string profile = await GetFfprobeInfoAsync(path, showStreams, "profile", idx);
                            if (codec.Lower() == "dts" && profile != "unknown") codec = profile;
                            string codecLong = await GetFfprobeInfoAsync(path, showStreams, "codec_long_name", idx);
                            int kbits = (await GetFfprobeInfoAsync(path, showStreams, "bit_rate", idx)).GetInt() / 1024;
                            int sampleRate = (await GetFfprobeInfoAsync(path, showStreams, "sample_rate", idx)).GetInt();
                            int channels = (await GetFfprobeInfoAsync(path, showStreams, "channels", idx)).GetInt();
                            string layout = (await GetFfprobeInfoAsync(path, showStreams, "channel_layout", idx));
                            AudioStream aStream = new AudioStream(lang, title, codec, codecLong, kbits, sampleRate, channels, layout);
                            aStream.Index = idx;
                            aStream.IsDefault = def;
                            Logger.Log($"Added audio stream: {aStream}", true);
                            streamList.Add(aStream);
                            continue;
                        }

                        if (streamStr.Contains(": Subtitle:"))
                        {
                            string lang = await GetFfprobeInfoAsync(path, showStreams, "TAG:language", idx);
                            string title = await GetFfprobeInfoAsync(path, showStreams, "TAG:title", idx);
                            string codec = await GetFfprobeInfoAsync(path, showStreams, "codec_name", idx);
                            string codecLong = await GetFfprobeInfoAsync(path, showStreams, "codec_long_name", idx);
                            bool bitmap = await IsSubtitleBitmapBased(path, idx, codec);
                            SubtitleStream sStream = new SubtitleStream(lang, title, codec, codecLong, bitmap);
                            sStream.Index = idx;
                            sStream.IsDefault = def;
                            Logger.Log($"Added subtitle stream: {sStream}", true);
                            streamList.Add(sStream);
                            continue;
                        }

                        if (streamStr.Contains(": Data:"))
                        {
                            string codec = await GetFfprobeInfoAsync(path, showStreams, "codec_name", idx);
                            string codecLong = await GetFfprobeInfoAsync(path, showStreams, "codec_long_name", idx);
                            DataStream dStream = new DataStream(codec, codecLong);
                            dStream.Index = idx;
                            dStream.IsDefault = def;
                            Logger.Log($"Added data stream: {dStream}", true);
                            streamList.Add(dStream);
                            continue;
                        }

                        if (streamStr.Contains(": Attachment:"))
                        {
                            string codec = await GetFfprobeInfoAsync(path, showStreams, "codec_name", idx);
                            string codecLong = await GetFfprobeInfoAsync(path, showStreams, "codec_long_name", idx);
                            string filename = await GetFfprobeInfoAsync(path, showStreams, "TAG:filename", idx);
                            string mimeType = await GetFfprobeInfoAsync(path, showStreams, "TAG:mimetype", idx);
                            AttachmentStream aStream = new AttachmentStream(codec, codecLong, filename, mimeType);
                            aStream.Index = idx;
                            aStream.IsDefault = def;
                            Logger.Log($"Added attachment stream: {aStream}", true);
                            streamList.Add(aStream);
                            continue;
                        }

                        Logger.Log($"Unknown stream (not vid/aud/sub/dat/att): {streamStr}", true);
                        Stream stream = new Stream { Codec = "Unknown", CodecLong = "Unknown", Index = idx, IsDefault = def, Type = Stream.StreamType.Unknown };
                        streamList.Add(stream);
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Error scanning stream: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"GetStreams Exception: {e.Message}\n{e.StackTrace}", true);
            }

            Logger.Log($"Video Streams: {string.Join(", ", streamList.Where(x => x.Type == Stream.StreamType.Video).Select(x => string.IsNullOrWhiteSpace(x.Title) ? "No Title" : x.Title))}", true);
            Logger.Log($"Audio Streams: {string.Join(", ", streamList.Where(x => x.Type == Stream.StreamType.Audio).Select(x => string.IsNullOrWhiteSpace(x.Title) ? "No Title" : x.Title))}", true);
            Logger.Log($"Subtitle Streams: {string.Join(", ", streamList.Where(x => x.Type == Stream.StreamType.Subtitle).Select(x => string.IsNullOrWhiteSpace(x.Title) ? "No Title" : x.Title))}", true);

            if (progressBar)
                Program.mainForm.SetProgress(0);

            return streamList;
        }

        private static async Task<FpsInfo> GetFps(string path, string streamStr, int streamIdx, Fraction defaultFps, int frameCount)
        {
            if (path.IsConcatFile())
                return new FpsInfo(defaultFps);

            if (streamStr.Contains("fps, ") && streamStr.Contains(" tbr"))
            {
                string fps = streamStr.Split(", ").Where(s => s.Contains(" fps")).First().Trim().Split(' ')[0];
                string tbr = streamStr.Split("fps, ")[1].Split(" tbr")[0].Trim();
                long durationMs = Interpolate.currentMediaFile.DurationMs;
                float fpsCalc = (float)frameCount / (durationMs / 1000f);
                fpsCalc = (float)Math.Round(fpsCalc, 5);

                var info = new FpsInfo(new Fraction(fps.GetFloat())); // Set both true FPS and average FPS to this number for now

                Logger.Log($"FPS: {fps} - TBR: {tbr} - Est. FPS: {fpsCalc.ToString("0.#####")}", true);

                if (tbr != fps)
                {
                    info.SpecifiedFps = new Fraction(tbr); // Change FPS to TBR if they mismatch
                }

                float fpsEstTolerance = GetFpsEstimationTolerance(durationMs);

                if (Math.Abs(fps.GetFloat() - fpsCalc) > fpsEstTolerance)
                {
                    Logger.Log($"Detected FPS {fps} is not within tolerance (+-{fpsEstTolerance}) of calculated FPS ({fpsCalc}), using estimated FPS.", true);
                    info.Fps = new Fraction(fpsCalc); // Change true FPS to the estimated FPS if the estimate does not match the specified FPS
                }

                return info;
            }

            return new FpsInfo(await IoUtils.GetVideoFramerate(path));
        }

        private static float GetFpsEstimationTolerance (long videoDurationMs)
        {
            if (videoDurationMs < 300) return 5.0f;
            if (videoDurationMs < 1000) return 2.5f;
            if (videoDurationMs < 2500) return 1.0f;
            if (videoDurationMs < 5000) return 0.75f;
            if (videoDurationMs < 10000) return 0.5f;
            if (videoDurationMs < 20000) return 0.25f;

            return 0.1f;
        }

        public static async Task<bool> IsSubtitleBitmapBased(string path, int streamIndex, string codec = "")
        {
            if (codec == "ssa" || codec == "ass" || codec == "mov_text" || codec == "srt" || codec == "subrip" || codec == "text" || codec == "webvtt")
                return false;

            if (codec == "dvdsub" || codec == "dvd_subtitle" || codec == "pgssub" || codec == "hdmv_pgs_subtitle" || codec.StartsWith("dvb_"))
                return true;

            // If codec was not listed above, manually check if it's compatible by trying to encode it:
            //string ffmpegCheck = await GetFfmpegOutputAsync(path, $"-map 0:{streamIndex} -c:s srt -t 0 -f null -");
            //return ffmpegCheck.Contains($"encoding currently only possible from text to text or bitmap to bitmap");

            return false;
        }

        public static string[] GetEncArgs(OutputSettings settings, Size res, float fps, bool forceSinglePass = false) // Array contains as many entries as there are encoding passes.
        {
            Encoder enc = settings.Encoder;
            int keyint = 10;
            var args = new List<string>();
            EncoderInfoVideo info = OutputUtils.GetEncoderInfoVideo(enc);
            PixelFormat pixFmt = settings.PixelFormat;

            if (settings.Format == Enums.Output.Format.Realtime)
                pixFmt = PixelFormat.Yuv444P16Le;

            if (pixFmt == (PixelFormat)(-1)) // No pixel format set in GUI
                pixFmt = info.PixelFormatDefault != (PixelFormat)(-1) ? info.PixelFormatDefault : info.PixelFormats.First(); // Set default or fallback to first in list

            args.Add($"-c:v {info.Name}");

            if (enc == Encoder.X264 || enc == Encoder.X265 || enc == Encoder.SvtAv1 || enc == Encoder.VpxVp9 || enc == Encoder.Nvenc264 || enc == Encoder.Nvenc265 || enc == Encoder.NvencAv1)
                args.Add(GetKeyIntArg(fps, keyint));

            if (enc == Encoder.X264)
            {
                string preset = Config.Get(Config.Key.ffEncPreset).Lower().Remove(" "); // TODO: Replace this ugly stuff with enums
                int crf = GetCrf(settings);
                args.Add($"-crf {crf} -preset {preset}");
            }

            if (enc == Encoder.X265)
            {
                string preset = Config.Get(Config.Key.ffEncPreset).Lower().Remove(" "); // TODO: Replace this ugly stuff with enums
                int crf = GetCrf(settings);
                args.Add($"{(crf > 0 ? $"-crf {crf}" : "-x265-params lossless=1")} -preset {preset}");
            }

            if (enc == Encoder.SvtAv1)
            {
                int crf = GetCrf(settings);
                args.Add($"-crf {crf} {GetSvtAv1Speed()} -svtav1-params enable-qm=1:enable-overlays=1:enable-tf=0:scd=0");
            }

            if (enc == Encoder.VpxVp9)
            {
                int crf = GetCrf(settings);
                string qualityStr = (crf > 0) ? $"-crf {crf}" : "-lossless 1";
                string t = GetTilingArgs(res, "-tile-columns ", "-tile-rows ");

                if (forceSinglePass) // Force 1-pass
                {
                    args.Add($"-b:v 0 {qualityStr} {GetVp9Speed()} {t} -row-mt 1");
                }
                else
                {
                    return new string[] {
                        $"{string.Join(" ", args)} -b:v 0 {qualityStr} {GetVp9Speed()} {t} -row-mt 1 -pass 1 -an",
                        $"{string.Join(" ", args)} -b:v 0 {qualityStr} {GetVp9Speed()} {t} -row-mt 1 -pass 2"
                    };
                }
            }

            // Fix NVENC pixel formats
            if (enc.ToString().StartsWith("Nvenc"))
            {
                if (pixFmt == PixelFormat.Yuv420P10Le) pixFmt = PixelFormat.P010Le;
            }

            if (enc == Encoder.Nvenc264)
            {
                int crf = GetCrf(settings);
                args.Add($"-b:v 0 -preset p7 {(crf > 0 ? $"-cq {crf}" : "-tune lossless")}");
            }

            if (enc == Encoder.Nvenc265)
            {
                int crf = GetCrf(settings);
                args.Add($"-b:v 0 -preset p7 {(crf > 0 ? $"-cq {crf}" : "-tune lossless")}");
            }

            if (enc == Encoder.NvencAv1)
            {
                int crf = GetCrf(settings);
                args.Add($"-b:v 0 -preset p7 -cq {crf}");
            }

            if (enc == Encoder.Amf264)
            {
                int crf = GetCrf(settings);
                args.Add($"-b:v 0 -rc cqp -qp_i {crf} -qp_p {crf} -quality 2");
            }

            if (enc == Encoder.Amf265)
            {
                int crf = GetCrf(settings);
                args.Add($"-b:v 0 -rc cqp -qp_i {crf} -qp_p {crf} -quality 2");
            }

            if (enc == Encoder.Qsv264)
            {
                int crf = GetCrf(settings).Clamp(1, 51);
                args.Add($"-preset veryslow -global_quality {crf}");
            }

            if (enc == Encoder.Qsv265)
            {
                int crf = GetCrf(settings).Clamp(1, 51);
                args.Add($"-preset veryslow -global_quality {crf}");
            }

            if (enc == Encoder.ProResKs)
            {
                var profile = ParseUtils.GetEnum<Quality.ProResProfile>(settings.Quality, true, Strings.VideoQuality);
                args.Add($"-profile:v {OutputUtils.ProresProfiles[profile]}");
            }

            if (enc == Encoder.Gif)
            {
                args.Add("-gifflags -offsetting");
            }

            if (enc == Encoder.Jpeg)
            {
                var qualityLevel = ParseUtils.GetEnum<Quality.JpegWebm>(settings.Quality, true, Strings.VideoQuality);
                args.Add($"-q:v {OutputUtils.JpegQuality[qualityLevel]}");
            }

            if (enc == Encoder.Webp)
            {
                var qualityLevel = ParseUtils.GetEnum<Quality.JpegWebm>(settings.Quality, true, Strings.VideoQuality);
                args.Add($"-q:v {OutputUtils.WebpQuality[qualityLevel]}");
            }

            if (enc == Encoder.Exr)
            {
                args.Add($"-format {settings.Quality.Lower()}");
            }

            if (pixFmt != (PixelFormat)(-1))
                args.Add($"-pix_fmt {pixFmt.ToString().Lower()}");

            return new string[] { string.Join(" ", args) };
        }

        private static int GetCrf(OutputSettings settings)
        {
            if (settings.CustomQuality.IsNotEmpty())
                return settings.CustomQuality.GetInt();
            else
                return OutputUtils.GetCrf(ParseUtils.GetEnum<Quality.Common>(settings.Quality, true, Strings.VideoQuality), settings.Encoder);
        }

        public static string GetTilingArgs(Size resolution, string colArg, string rowArg)
        {
            int cols = 0;
            if (resolution.Width >= 1920) cols = 1;
            if (resolution.Width >= 3840) cols = 2;
            if (resolution.Width >= 7680) cols = 3;

            int rows = 0;
            if (resolution.Height >= 1600) rows = 1;
            if (resolution.Height >= 3200) rows = 2;
            if (resolution.Height >= 6400) rows = 3;

            Logger.Log($"GetTilingArgs: Video resolution is {resolution.Width}x{resolution.Height} - Using 2^{cols} columns, 2^{rows} rows (=> {Math.Pow(2, cols)}x{Math.Pow(2, rows)} = {Math.Pow(2, cols) * Math.Pow(2, rows)} Tiles)", true);

            return $"{(cols > 0 ? colArg + cols : "")} {(rows > 0 ? rowArg + rows : "")}";
        }

        public static string GetKeyIntArg(float fps, int intervalSeconds, string arg = "-g ")
        {
            int keyInt = (fps * intervalSeconds).RoundToInt().Clamp(30, 600);
            return $"{arg}{keyInt}";
        }

        static string GetVp9Speed()
        {
            string preset = Config.Get(Config.Key.ffEncPreset).Lower().Remove(" ");
            string arg = "";

            if (preset == "veryslow") arg = "0";
            if (preset == "slower") arg = "1";
            if (preset == "slow") arg = "2";
            if (preset == "medium") arg = "3";
            if (preset == "fast") arg = "4";
            if (preset == "faster") arg = "5";
            if (preset == "veryfast") arg = "4 -deadline realtime";

            return $"-cpu-used {arg}";
        }

        static string GetSvtAv1Speed()
        {
            string preset = Config.Get(Config.Key.ffEncPreset).Lower().Remove(" ");
            string arg = "8";

            if (preset == "veryslow") arg = "3";
            if (preset == "slower") arg = "4";
            if (preset == "slow") arg = "5";
            if (preset == "medium") arg = "6";
            if (preset == "fast") arg = "7";
            if (preset == "faster") arg = "8";
            if (preset == "veryfast") arg = "9";

            return $"-preset {arg}";
        }

        public static bool ContainerSupportsAllAudioFormats(Enums.Output.Format outFormat, List<string> codecs)
        {
            if (codecs.Count < 1)
                Logger.Log($"Warning: ContainerSupportsAllAudioFormats() was called, but codec list has {codecs.Count} entries.", true, false, "ffmpeg");

            foreach (string format in codecs)
            {
                if (!ContainerSupportsAudioFormat(outFormat, format))
                    return false;
            }

            return true;
        }

        public static bool ContainerSupportsAudioFormat(Enums.Output.Format outFormat, string format)
        {
            bool supported = false;
            string alias = GetAudioExt(format);

            string[] formatsMp4 = new string[] { "m4a", "mp3", "ac3", "dts" };
            string[] formatsMkv = new string[] { "m4a", "mp3", "ac3", "dts", "ogg", "mp2", "wav", "wma" };
            string[] formatsWebm = new string[] { "ogg" };
            string[] formatsMov = new string[] { "m4a", "ac3", "dts", "wav" };
            string[] formatsAvi = new string[] { "m4a", "ac3", "dts" };

            switch (outFormat)
            {
                case Enums.Output.Format.Mp4: supported = formatsMp4.Contains(alias); break;
                case Enums.Output.Format.Mkv: supported = formatsMkv.Contains(alias); break;
                case Enums.Output.Format.Webm: supported = formatsWebm.Contains(alias); break;
                case Enums.Output.Format.Mov: supported = formatsMov.Contains(alias); break;
                case Enums.Output.Format.Avi: supported = formatsAvi.Contains(alias); break;
            }

            Logger.Log($"Checking if {outFormat} supports audio format '{format}' ({alias}): {supported}", true, false, "ffmpeg");
            return supported;
        }

        public static string GetExt(OutputSettings settings, bool dot = true)
        {
            string ext = dot ? "." : "";
            EncoderInfoVideo info = settings.Encoder.GetInfo();

            if (string.IsNullOrWhiteSpace(info.OverideExtension))
                ext += settings.Format.ToString().Lower();
            else
                ext += info.OverideExtension;

            return ext;
        }

        public static string GetAudioExt(string codec)
        {
            if (codec.StartsWith("pcm_"))
                return "wav";

            switch (codec)
            {
                case "vorbis": return "ogg";
                case "opus": return "ogg";
                case "mp2": return "mp2";
                case "mp3": return "mp3";
                case "aac": return "m4a";
                case "ac3": return "ac3";
                case "eac3": return "ac3";
                case "dts": return "dts";
                case "alac": return "wav";
                case "flac": return "wav";
                case "wmav1": return "wma";
                case "wmav2": return "wma";
            }

            return "unsupported";
        }

        public static async Task<string> GetAudioFallbackArgs(string videoPath, Enums.Output.Format outFormat, float itsScale)
        {
            bool opusMp4 = Config.GetBool(Config.Key.allowOpusInMp4);
            int opusBr = Config.GetInt(Config.Key.opusBitrate, 128);
            int aacBr = Config.GetInt(Config.Key.aacBitrate, 160);
            int ac = (await GetVideoInfo.GetFfprobeInfoAsync(videoPath, GetVideoInfo.FfprobeMode.ShowStreams, "channels", 0)).GetInt();
            string af = GetAudioFilters(itsScale);

            if (outFormat == Enums.Output.Format.Mkv || outFormat == Enums.Output.Format.Webm || (outFormat == Enums.Output.Format.Mp4 && opusMp4))
                return $"-c:a libopus -b:a {(ac > 4 ? $"{opusBr * 2}" : $"{opusBr}")}k -ac {(ac > 0 ? $"{ac}" : "2")} {af}"; // Double bitrate if 5ch or more, ignore ac if <= 0
            else
                return $"-c:a aac -b:a {(ac > 4 ? $"{aacBr * 2}" : $"{aacBr}")}k -aac_coder twoloop -ac {(ac > 0 ? $"{ac}" : "2")} {af}";
        }

        private static string GetAudioFilters(float itsScale)
        {
            if (itsScale == 0 || itsScale == 1)
                return "";

            if (itsScale > 4)
                return $"-af atempo=0.5,atempo=0.5,atempo={((1f / itsScale) * 4).ToStringDot()}";
            else if (itsScale > 2)
                return $"-af atempo=0.5,atempo={((1f / itsScale) * 2).ToStringDot()}";
            else
                return $"-af atempo={(1f / itsScale).ToStringDot()}";
        }

        public static string GetSubCodecForContainer(string containerExt)
        {
            containerExt = containerExt.Remove(".");

            if (containerExt == "mp4" || containerExt == "mov") return "mov_text";
            if (containerExt == "webm") return "webvtt";

            return "copy";    // Default: Copy subs
        }

        public static bool ContainerSupportsSubs(string containerExt, bool showWarningIfNotSupported = true)
        {
            containerExt = containerExt.Remove(".");
            bool supported = (containerExt == "mp4" || containerExt == "mkv" || containerExt == "webm" || containerExt == "mov");
            Logger.Log($"Subtitles {(supported ? "are supported" : "not supported")} by {containerExt.Upper()}", true);

            if (showWarningIfNotSupported && Config.GetBool(Config.Key.keepSubs) && !supported)
                Logger.Log($"Warning: {containerExt.Upper()} exports do not include subtitles.");

            return supported;
        }

        public static int CreateConcatFile(string inputFilesDir, string outputPath, List<string> validExtensions = null)
        {
            if (IoUtils.GetAmountOfFiles(inputFilesDir, false) < 1)
                return 0;

            Directory.CreateDirectory(outputPath.GetParentDir());
            validExtensions = validExtensions ?? new List<string>();
            validExtensions = validExtensions.Select(x => x.Remove(".").Lower()).ToList(); // Ignore "." in extensions
            var validFiles = IoUtils.GetFilesSorted(inputFilesDir).Where(f => validExtensions.Contains(Path.GetExtension(f).Replace(".", "").Lower()));
            string fileContent = string.Join(Environment.NewLine, validFiles.Select(f => $"file '{f.Replace(@"\", "/")}'"));
            IoUtils.TryDeleteIfExists(outputPath);
            File.WriteAllText(outputPath, fileContent);

            return validFiles.Count();
        }

        public static Size SizeFromString(string str, char delimiter = ':')
        {
            try
            {
                if (str.IsEmpty() || str.Length < 3 || !str.Contains(delimiter))
                    return new Size();

                string[] nums = str.Remove(" ").Trim().Split(delimiter);
                return new Size(nums[0].GetInt(), nums[1].GetInt());
            }
            catch
            {
                return new Size();
            }
        }
    }
}
