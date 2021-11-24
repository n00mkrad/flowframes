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
using Microsoft.VisualBasic.Logging;

namespace Flowframes
{
    public class InterpSettings
    {
        public string inPath;
        public string outPath;
        public AI ai;
        public Fraction inFps;
        public Fraction inFpsDetected;
        public Fraction outFps;
        public float outItsScale;
        public float interpFactor;
        public Interpolate.OutMode outMode;
        public ModelCollection.ModelInfo model;

        public string tempFolder;
        public string framesFolder;
        public string interpFolder;
        public bool inputIsFrames;

        private Size _inputResolution;
        public Size InputResolution { get { RefreshInputRes(); return _inputResolution; } }
        private Size _scaledResolution;
        public Size ScaledResolution { get { RefreshOutputRes(); return _scaledResolution; } }

        public bool alpha;
        public bool stepByStep;

        public string framesExt;
        public string interpExt;

        public InterpSettings() { }

        public InterpSettings(string inPathArg, string outPathArg, AI aiArg, Fraction inFpsDetectedArg, Fraction inFpsArg, int interpFactorArg, float itsScale, Interpolate.OutMode outModeArg, ModelCollection.ModelInfo modelArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            ai = aiArg;
            inFpsDetected = inFpsDetectedArg;
            inFps = inFpsArg;
            interpFactor = interpFactorArg;
            outFps = inFpsArg * interpFactorArg;
            outItsScale = itsScale;
            outMode = outModeArg;
            model = modelArg;

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
            _scaledResolution = new Size(0, 0);

            RefreshExtensions();
        }

        public InterpSettings (string serializedData)
        {
            inPath = "";
            outPath = "";
            ai = Implementations.networks[0];
            inFpsDetected = new Fraction();
            inFps = new Fraction();
            interpFactor = 0;
            outFps = new Fraction();
            outMode = Interpolate.OutMode.VidMp4;
            model = null;
            alpha = false;
            stepByStep = false;
            _inputResolution = new Size(0, 0);
            _scaledResolution = new Size(0, 0);
            framesExt = "";
            interpExt = "";

            Dictionary<string, string> entries = new Dictionary<string, string>();

            foreach(string line in serializedData.SplitIntoLines())
            {
                if (line.Length < 3) continue;
                string[] keyValuePair = line.Split('|');
                entries.Add(keyValuePair[0], keyValuePair[1]);
            }

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
                    case "OUTMODE": outMode = (Interpolate.OutMode)Enum.Parse(typeof(Interpolate.OutMode), entry.Value); break;
                    case "MODEL": model = AiModels.GetModelByName(ai, entry.Value); break;
                    case "INPUTRES": _inputResolution = FormatUtils.ParseSize(entry.Value); break;
                    case "OUTPUTRES": _scaledResolution = FormatUtils.ParseSize(entry.Value); break;
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

        void RefreshOutputRes ()
        {
            if (_scaledResolution.IsEmpty)
                _scaledResolution = InterpolateUtils.GetOutputResolution(InputResolution, false, true);
        }

        public void RefreshAlpha ()
        {
            try
            {
                bool alphaModel = model.supportsAlpha;
                bool png = outMode == Interpolate.OutMode.ImgPng;
                bool gif = outMode == Interpolate.OutMode.VidGif;
                bool proResAlpha = outMode == Interpolate.OutMode.VidProRes && Config.GetInt(Config.Key.proResProfile) > 3;
                bool outputSupportsAlpha = png || gif || proResAlpha;
                string ext = inputIsFrames ? Path.GetExtension(IoUtils.GetFilesSorted(inPath).First()).ToLower() : Path.GetExtension(inPath).ToLower();
                alpha = (alphaModel && outputSupportsAlpha && (ext == ".gif" || ext == ".png" || ext == ".apng" || ext == ".mov"));
                Logger.Log($"RefreshAlpha: model.supportsAlpha = {alphaModel} - outputSupportsAlpha = {outputSupportsAlpha} - " +
                           $"input ext: {ext} => alpha = {alpha}", true);
            }
            catch (Exception e)
            {
                Logger.Log("RefreshAlpha Error: " + e.Message, true);
                alpha = false;
            }
        }

        public enum FrameType { Import, Interp, Both };

        public void RefreshExtensions(FrameType type = FrameType.Both)
        {
            bool pngOutput = outMode == Interpolate.OutMode.ImgPng;
            bool aviHqChroma = outMode == Interpolate.OutMode.VidAvi && Config.Get(Config.Key.aviColors) != "yuv420p";
            bool proresHqChroma = outMode == Interpolate.OutMode.VidProRes && Config.GetInt(Config.Key.proResProfile) > 3;

            bool forceHqChroma = pngOutput || aviHqChroma || proresHqChroma;

            Logger.Log($"RefreshExtensions({type}) - alpha = {alpha} pngOutput = {pngOutput} aviHqChroma = {aviHqChroma} proresHqChroma = {proresHqChroma}", true);

            if (alpha || forceHqChroma)     // Force PNG if alpha is enabled, or output is not 4:2:0 subsampled
            {
                if(type == FrameType.Both || type == FrameType.Import)
                    framesExt = ".png";

                if (type == FrameType.Both || type == FrameType.Interp)
                    interpExt = ".png";
            }
            else
            {
                if (type == FrameType.Both || type == FrameType.Import)
                    framesExt = (Config.GetBool(Config.Key.jpegFrames) ? ".jpg" : ".png");

                if (type == FrameType.Both || type == FrameType.Interp)
                    interpExt = (Config.GetBool(Config.Key.jpegInterp) ? ".jpg" : ".png");
            }

            Logger.Log($"RefreshExtensions - Using '{framesExt}' for imported frames, using '{interpExt}' for interpolated frames", true);
        }

        public string Serialize ()
        {
            string s = $"INPATH|{inPath}\n";
            s += $"OUTPATH|{outPath}\n";
            s += $"AI|{ai.aiName}\n";
            s += $"INFPSDETECTED|{inFpsDetected}\n";
            s += $"INFPS|{inFps}\n";
            s += $"OUTFPS|{outFps}\n";
            s += $"INTERPFACTOR|{interpFactor}\n";
            s += $"OUTMODE|{outMode}\n";
            s += $"MODEL|{model.name}\n";
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
