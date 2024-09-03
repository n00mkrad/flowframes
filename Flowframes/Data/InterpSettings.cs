using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes
{
    public class InterpSettings
    {
        public string inPath;
        public string outPath;
        public string FullOutPath { get; set; } = "";
        public AI ai;
        public string inPixFmt = "yuv420p";
        public Fraction inFps;
        public Fraction inFpsDetected;
        public Fraction outFps;
        public float outItsScale;
        public float interpFactor;
        public OutputSettings outSettings;
        public ModelCollection.ModelInfo model;

        public string tempFolder;
        public string framesFolder;
        public string interpFolder;
        public bool inputIsFrames;

        private Size _inputResolution;
        public Size InputResolution { get { RefreshInputRes(); return _inputResolution; } }
        public Size ScaledResolution { get { return InterpolateUtils.GetOutputResolution(InputResolution, false); } }
        public Size ScaledPaddedResolution { get { return InterpolateUtils.GetOutputResolution(InputResolution, true); } }

        public bool alpha;
        public bool stepByStep;

        public string framesExt;
        public string interpExt;

        public InterpSettings() { }

        public InterpSettings(string inPathArg, string outPathArg, AI aiArg, Fraction inFpsDetectedArg, Fraction inFpsArg, float interpFactorArg, float itsScale, OutputSettings outSettingsArg, ModelCollection.ModelInfo modelArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            ai = aiArg;
            inFpsDetected = inFpsDetectedArg;
            inFps = inFpsArg;
            interpFactor = interpFactorArg;
            outItsScale = itsScale;
            outSettings = outSettingsArg;
            model = modelArg;

            InitArgs();
        }

        public void InitArgs ()
        {
            outFps = inFps * (double)interpFactor;

            alpha = false;
            stepByStep = false;

            framesExt = "";
            interpExt = "";

            try
            {
                tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
                framesFolder = Path.Combine(tempFolder, Paths.framesDir);
                interpFolder = Path.Combine(tempFolder, Paths.interpDir);
                inputIsFrames = IoUtils.IsPathDirectory(inPath);
            }
            catch
            {
                Logger.Log("Tried to create InterpSettings struct without an inpath. Can't set tempFolder, framesFolder and interpFolder.", true);
                tempFolder = "";
                framesFolder = "";
                interpFolder = "";
                inputIsFrames = false;
            }

            _inputResolution = new Size(0, 0);

            RefreshExtensions(ai: ai);
        }

        public InterpSettings (string serializedData)
        {
            inPath = "";
            outPath = "";
            ai = null;
            inFpsDetected = new Fraction();
            inFps = new Fraction();
            interpFactor = 0;
            outFps = new Fraction();
            outSettings = new OutputSettings();
            model = null;
            alpha = false;
            stepByStep = false;
            _inputResolution = new Size(0, 0);
            framesExt = "";
            interpExt = "";

            Dictionary<string, string> entries = new Dictionary<string, string>();

            foreach(string line in serializedData.SplitIntoLines())
            {
                if (line.Length < 3) continue;
                string[] keyValuePair = line.Split('|');
                entries.Add(keyValuePair[0], keyValuePair[1]);
            }

            // TODO: Rework this ugly stuff, JSON?
            foreach (KeyValuePair<string, string> entry in entries)
            {
                switch (entry.Key)
                {
                    case "INPATH": inPath = entry.Value; break;
                    case "OUTPATH": outPath = entry.Value; break;
                    case "AI": ai = Implementations.GetAi(entry.Value); break;
                    case "INFPSDETECTED": inFpsDetected = new Fraction(entry.Value); break;
                    case "INFPS": inFps = new Fraction(entry.Value); break;
                    case "OUTFPS": outFps = new Fraction(entry.Value); break;
                    case "INTERPFACTOR": interpFactor = float.Parse(entry.Value); break;
                    case "OUTMODE": outSettings.Format = (Enums.Output.Format)Enum.Parse(typeof(Enums.Output.Format), entry.Value); break;
                    case "MODEL": model = AiModels.GetModelByName(ai, entry.Value); break;
                    case "INPUTRES": _inputResolution = FormatUtils.ParseSize(entry.Value); break;
                    case "ALPHA": alpha = bool.Parse(entry.Value); break;
                    case "STEPBYSTEP": stepByStep = bool.Parse(entry.Value); break;
                    case "FRAMESEXT": framesExt = entry.Value; break;
                    case "INTERPEXT": interpExt = entry.Value; break;
                }
            }

            try
            {
                tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
                framesFolder = Path.Combine(tempFolder, Paths.framesDir);
                interpFolder = Path.Combine(tempFolder, Paths.interpDir);
                inputIsFrames = IoUtils.IsPathDirectory(inPath);
            }
            catch
            {
                Logger.Log("Tried to create InterpSettings struct without an inpath. Can't set tempFolder, framesFolder and interpFolder.", true);
                tempFolder = "";
                framesFolder = "";
                interpFolder = "";
                inputIsFrames = false;
            }

            RefreshExtensions();
        }

        public void UpdatePaths (string inPathArg, string outPathArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
            framesFolder = Path.Combine(tempFolder, Paths.framesDir);
            interpFolder = Path.Combine(tempFolder, Paths.interpDir);
            inputIsFrames = IoUtils.IsPathDirectory(inPath);
        }

        async Task RefreshInputRes ()
        {
            if (_inputResolution.IsEmpty)
                _inputResolution = await GetMediaResolutionCached.GetSizeAsync(inPath);
        }

        public void RefreshAlpha ()
        {
            try
            {
                bool alphaModel = model.SupportsAlpha;
                bool pngOutput = outSettings.Encoder == Enums.Encoding.Encoder.Png;
                bool gifOutput = outSettings.Encoder == Enums.Encoding.Encoder.Gif;
                bool proResAlpha = outSettings.Encoder == Enums.Encoding.Encoder.ProResKs && OutputUtils.AlphaFormats.Contains(outSettings.PixelFormat);
                bool outputSupportsAlpha = pngOutput || gifOutput || proResAlpha;
                string ext = inputIsFrames ? Path.GetExtension(IoUtils.GetFilesSorted(inPath).First()).Lower() : Path.GetExtension(inPath).Lower();
                alpha = (alphaModel && outputSupportsAlpha && (ext == ".gif" || ext == ".png" || ext == ".apng" || ext == ".mov"));
                Logger.Log($"RefreshAlpha: model.supportsAlpha = {alphaModel} - outputSupportsAlpha = {outputSupportsAlpha} - input ext: {ext} => alpha = {alpha}", true);
            }
            catch (Exception e)
            {
                Logger.Log("RefreshAlpha Error: " + e.Message, true);
                alpha = false;
            }
        }

        public enum FrameType { Import, Interp, Both };

        public void RefreshExtensions(FrameType type = FrameType.Both, AI ai = null)
        {
            if(ai == null)
            {
                if (Interpolate.currentSettings == null)
                    return;

                ai = Interpolate.currentSettings.ai;
            }

            bool pngOutput = outSettings.Encoder == Enums.Encoding.Encoder.Png;
            bool aviHqChroma = outSettings.Format == Enums.Output.Format.Avi && OutputUtils.AlphaFormats.Contains(outSettings.PixelFormat);
            bool proresHqChroma = outSettings.Encoder == Enums.Encoding.Encoder.ProResKs && OutputUtils.AlphaFormats.Contains(outSettings.PixelFormat);
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

        public string Serialize ()
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
            s += $"OUTPUTRES|{ScaledResolution.Width}x{ScaledResolution.Height}\n";
            s += $"ALPHA|{alpha}\n";
            s += $"STEPBYSTEP|{stepByStep}\n";
            s += $"FRAMESEXT|{framesExt}\n";
            s += $"INTERPEXT|{interpExt}\n";

            return s;
        }
    }
}
