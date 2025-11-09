using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using Enc = Flowframes.Data.Enums.Encoding.Encoder;

namespace Flowframes
{
    public class InterpSettings
    {
        public string inPath;
        public string outPath;
        public string FullOutPath { get; set; } = "";
        public AiInfo ai;
        public Fraction inFps;
        public Fraction inFpsDetected;
        public Fraction outFps;
        public Fraction outFpsResampled;
        public bool FpsResampling => outFpsResampled != null && outFpsResampled.Float > 0.1f && outFpsResampled.Float < outFps.Float;
        public float outItsScale;
        public float interpFactor;
        public OutputSettings outSettings;
        public ModelCollection.ModelInfo model;

        public string tempFolder;
        public string framesFolder;
        public string interpFolder;
        public bool inputIsFrames;
        public bool dedupe;
        public bool noRedupe;

        private Size _inputResolution = new Size();
        public Size InputResolution
        {
            get
            {
                if (_inputResolution.IsEmpty)
                    _inputResolution = GetMediaResolutionCached.GetSizeAsync(inPath).Result;
                return _inputResolution;
            }
        }

        private Size _outputResolution = new Size();
        public Size OutputResolution
        {
            get
            {
                if (_outputResolution.IsEmpty)
                    _outputResolution = InterpolateUtils.GetInterpolationResolution(FfmpegCommands.ModuloMode.ForEncoding, InputResolution);
                return _outputResolution;
            }
        }

        private Size _scaledResolution = new Size();
        public Size ScaledResolution
        {
            get
            {
                if (_scaledResolution.IsEmpty)
                    _scaledResolution = InterpolateUtils.GetInterpolationResolution(FfmpegCommands.ModuloMode.Disabled, InputResolution);
                return _scaledResolution;
            }
        }

        private Size _interpResolution = new Size();
        public Size InterpResolution
        {
            get
            {
                if (_interpResolution.IsEmpty)
                    _interpResolution = InterpolateUtils.GetInterpolationResolution(FfmpegCommands.ModuloMode.ForInterpolation, InputResolution);
                return _interpResolution;
            }
        }

        public bool alpha;
        public bool stepByStep;

        public string framesExt;
        public string interpExt;

        public InterpSettings() { }

        public void InitArgs()
        {
            outFps = inFps == null ? new Fraction() : inFps * (double)interpFactor;
            outFpsResampled = new Fraction(Config.Get(Config.Key.maxFps));
            alpha = false;
            stepByStep = false;
            framesExt = "";
            interpExt = "";
            _inputResolution = new Size(0, 0);
            noRedupe = dedupe && interpFactor == 1;
            SetPaths(inPath);
            RefreshExtensions(ai: ai);
        }

        private void SetPaths(string inputPath)
        {
            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
                return;

            inPath = inputPath;
            // outPath = (Config.GetInt("outFolderLoc") == 0) ? inputPath.GetParentDir() : Config.Get("custOutDir").Trim();
            tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
            framesFolder = Path.Combine(tempFolder, Paths.framesDir);
            interpFolder = Path.Combine(tempFolder, Paths.interpDir);
            inputIsFrames = IoUtils.IsPathDirectory(inPath);
        }

        public void RefreshAlpha(bool vs = false)
        {
            try
            {
                if (Interpolate.currentMediaFile != null && !Interpolate.currentMediaFile.MayHaveAlpha)
                {
                    Logger.Log($"RefreshAlpha: Input video does not have alpha channel.", true);
                    return;
                }

                string noAlphaReason = CantUseAlphaReason(vs);
                alpha = string.IsNullOrEmpty(noAlphaReason);
                Logger.Log($"RefreshAlpha: Alpha {(alpha ? "enabled" : $"disabled: {noAlphaReason}")}", true);

                if (!alpha)
                {
                    Logger.Log($"Input may have alpha (transparency) which won't be interpolated - {noAlphaReason}");
                }
                else
                {
                    Logger.Log($"Input has alpha (transparency), interpolation will take longer. Alpha support can be toggled in the Settings.");
                }
            }
            catch (Exception e)
            {
                Logger.Log("RefreshAlpha Error: " + e.Message, true);
                alpha = false;
            }
        }

        private string CantUseAlphaReason(bool vs)
        {
            if (!Config.GetBool(Config.Key.enableAlpha))
                return "Alpha interpolation is disabled.";

            if (!vs && !model.SupportsAlpha)
                return "Alpha requires VS backend or a compatible model.";

            var supportedEncoders = new List<Enc> { Enc.Png, Enc.Gif, Enc.Webp, Enc.Exr, Enc.Tiff, Enc.ProResKs, Enc.VpxVp9, Enc.Ffv1, Enc.Huffyuv };

            if(!supportedEncoders.Contains(outSettings.Encoder))
                return $"Selected encoder ({Strings.Encoder.Get($"{outSettings.Encoder}", returnKeyInsteadOfEmptyString: true)}) does not support alpha.";

            if(!OutputUtils.AlphaFormats.Contains(outSettings.PixelFormat))
                return $"Selected pixel format ({Strings.PixelFormat.Get($"{outSettings.PixelFormat}", returnKeyInsteadOfEmptyString: true)}) does not support alpha.";

            return "";
        }

        public enum FrameType { Import, Interp, Both };

        public void RefreshExtensions(FrameType type = FrameType.Both, AiInfo ai = null)
        {
            if (ai == null)
            {
                if (Interpolate.currentSettings == null)
                    return;

                ai = Interpolate.currentSettings.ai;
            }

            bool pngOutput = outSettings.Encoder == Enc.Png;
            bool aviHqChroma = outSettings.Format == Enums.Output.Format.Avi && OutputUtils.AlphaFormats.Contains(outSettings.PixelFormat);
            bool proresHqChroma = outSettings.Encoder == Enc.ProResKs && OutputUtils.AlphaFormats.Contains(outSettings.PixelFormat);
            bool forceHqChroma = pngOutput || aviHqChroma || proresHqChroma;
            bool tiffSupport = !ai.NameInternal.Upper().EndsWith("NCNN"); // NCNN binaries can't load TIFF (unlike OpenCV, ffmpeg etc)
            string losslessExt = tiffSupport ? ".tiff" : ".png";
            bool allowJpegImport = Config.GetBool(Config.Key.jpegFrames) && !(alpha || forceHqChroma); // Force PNG if alpha is enabled, or output is not 4:2:0 subsampled
            bool allowJpegExport = Config.GetBool(Config.Key.jpegInterp) && !(alpha || forceHqChroma);

            Logger.Log($"RefreshExtensions({type}) - alpha = {alpha} pngOutput = {pngOutput} aviHqChroma = {aviHqChroma} proresHqChroma = {proresHqChroma}", true);

            if (type == FrameType.Both || type == FrameType.Import)
                framesExt = allowJpegImport ? ".jpg" : losslessExt;

            if (type == FrameType.Both || type == FrameType.Interp)
                interpExt = allowJpegExport ? ".jpg" : ".png";

            Logger.Log($"RefreshExtensions - Using '{framesExt}' for imported frames, using '{interpExt}' for interpolated frames", true);
        }

        public string Serialize()
        {
            string s = $"INPATH|{inPath}\n";
            s += $"OUTPATH|{outPath}\n";
            s += $"AI|{ai.NameInternal}\n";
            s += $"INFPSDETECTED|{inFpsDetected}\n";
            s += $"INFPS|{inFps}\n";
            s += $"OUTFPS|{outFps}\n";
            s += $"INTERPFACTOR|{interpFactor}\n";
            s += $"OUTMODE|{outSettings.Format}\n";
            s += $"MODEL|{model.Name}\n";
            s += $"INPUTRES|{InputResolution.Width}x{InputResolution.Height}\n";
            s += $"OUTPUTRES|{OutputResolution.Width}x{OutputResolution.Height}\n";
            s += $"ALPHA|{alpha}\n";
            s += $"STEPBYSTEP|{stepByStep}\n";
            s += $"FRAMESEXT|{framesExt}\n";
            s += $"INTERPEXT|{interpExt}\n";

            return s;
        }
    }
}
