using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Logging;

namespace Flowframes
{
    public struct InterpSettings
    {
        public string inPath;
        public string outPath;
        public AI ai;
        public Fraction inFps;
        public Fraction inFpsDetected;
        public Fraction outFps;
        public float interpFactor;
        public Interpolate.OutMode outMode;
        public string model;

        public string tempFolder;
        public string framesFolder;
        public string interpFolder;
        public bool inputIsFrames;
        public Size inputResolution;
        public Size scaledResolution;

        public bool alpha;
        public bool stepByStep;

        public string framesExt;
        public string interpExt;

        public InterpSettings(string inPathArg, string outPathArg, AI aiArg, Fraction inFpsDetectedArg, Fraction inFpsArg, int interpFactorArg, Interpolate.OutMode outModeArg, string modelArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            ai = aiArg;
            inFpsDetected = inFpsDetectedArg;
            inFps = inFpsArg;
            interpFactor = interpFactorArg;
            outFps = inFpsArg * interpFactorArg;
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
                inputIsFrames = IOUtils.IsPathDirectory(inPath);
            }
            catch
            {
                Logger.Log("Tried to create InterpSettings struct without an inpath. Can't set tempFolder, framesFolder and interpFolder.", true);
                tempFolder = "";
                framesFolder = "";
                interpFolder = "";
                inputIsFrames = false;
            }

            inputResolution = new Size(0, 0);
            scaledResolution = new Size(0, 0);

            RefreshExtensions();
        }

        public InterpSettings (string serializedData)
        {
            inPath = "";
            outPath = "";
            ai = Networks.networks[0];
            inFpsDetected = new Fraction();
            inFps = new Fraction();
            interpFactor = 0;
            outFps = new Fraction();
            outMode = Interpolate.OutMode.VidMp4;
            model = "";
            alpha = false;
            stepByStep = false;
            inputResolution = new Size(0, 0);
            scaledResolution = new Size(0, 0);
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
                    case "AI": ai = Networks.GetAi(entry.Value); break;
                    case "INFPSDETECTED": inFpsDetected = new Fraction(entry.Value); break;
                    case "INFPS": inFps = new Fraction(entry.Value); break;
                    case "OUTFPS": outFps = new Fraction(entry.Value); break;
                    case "INTERPFACTOR": interpFactor = float.Parse(entry.Value); break;
                    case "OUTMODE": outMode = (Interpolate.OutMode)Enum.Parse(typeof(Interpolate.OutMode), entry.Value); break;
                    case "MODEL": model = entry.Value; break;
                    case "INPUTRES": inputResolution = FormatUtils.ParseSize(entry.Value); break;
                    case "OUTPUTRES": scaledResolution = FormatUtils.ParseSize(entry.Value); break;
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
                inputIsFrames = IOUtils.IsPathDirectory(inPath);
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
            inputIsFrames = IOUtils.IsPathDirectory(inPath);
        }

        public async Task<Size> GetInputRes()
        {
            await RefreshResolutions();
            return inputResolution;
        }

        public async Task<Size> GetScaledRes()
        {
            await RefreshResolutions();
            return scaledResolution;
        }

        async Task RefreshResolutions ()
        {
            if (inputResolution.IsEmpty || scaledResolution.IsEmpty)
            {
                inputResolution = await GetMediaResolutionCached.GetSizeAsync(inPath);
                scaledResolution = InterpolateUtils.GetOutputResolution(inputResolution, false, true);
            }
        }

        public void RefreshAlpha ()
        {
            try
            {
                bool alphaEnabled = Config.GetBool("enableAlpha", false);
                bool outputSupportsAlpha = (outMode == Interpolate.OutMode.ImgPng || outMode == Interpolate.OutMode.VidGif);
                string ext = inputIsFrames ? Path.GetExtension(IOUtils.GetFilesSorted(inPath).First()).ToLower() : Path.GetExtension(inPath).ToLower();
                alpha = (alphaEnabled && outputSupportsAlpha && (ext == ".gif" || ext == ".png" || ext == ".apng"));
                Logger.Log($"RefreshAlpha: alphaEnabled = {alphaEnabled} - outputSupportsAlpha = {outputSupportsAlpha} - " +
                           $"input ext: {ext} => alpha = {alpha}", true);
            }
            catch (Exception e)
            {
                Logger.Log("RefreshAlpha Error: " + e.Message, true);
                alpha = false;
            }
        }

        public void RefreshExtensions()
        {
            bool pngOutput = outMode == Interpolate.OutMode.ImgPng;
            bool aviHqChroma = outMode == Interpolate.OutMode.VidAvi && Config.Get("aviColors") != "yuv420p";
            bool proresHqChroma = outMode == Interpolate.OutMode.VidProRes && Config.GetInt("proResProfile") > 3;

            bool forceHqChroma = pngOutput || aviHqChroma || proresHqChroma;

            if (alpha || forceHqChroma)     // Force PNG if alpha is enabled, or output is not 4:2:0 subsampled
            {
                framesExt = ".png";
                interpExt = ".png";
            }
            else
            {
                framesExt = (Config.GetBool("jpegFrames") ? ".jpg" : ".png");
                interpExt = (Config.GetBool("jpegInterp") ? ".jpg" : ".png");
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
            s += $"MODEL|{model}\n";
            s += $"INPUTRES|{inputResolution.Width}x{inputResolution.Height}\n";
            s += $"OUTPUTRES|{scaledResolution.Width}x{scaledResolution.Height}\n";
            s += $"ALPHA|{alpha}\n";
            s += $"STEPBYSTEP|{stepByStep}\n";
            s += $"FRAMESEXT|{framesExt}\n";
            s += $"INTERPEXT|{interpExt}\n";

            return s;
        }
    }
}
