using Flowframes.AudioVideo;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Flowframes
{
    public struct InterpSettings
    {
        public string inPath;
        public string outPath;
        public AI ai;
        public float inFps;
        public float outFps;
        public int interpFactor;
        public Interpolate.OutMode outMode;
        public string model;

        public string tempFolder;
        public string framesFolder;
        public string interpFolder;
        public bool inputIsFrames;
        public string outFilename;
        public Size inputResolution;
        public Size scaledResolution;

        public bool alpha;

        public InterpSettings(string inPathArg, string outPathArg, AI aiArg, float inFpsArg, int interpFactorArg, Interpolate.OutMode outModeArg, string modelArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            ai = aiArg;
            inFps = inFpsArg;
            interpFactor = interpFactorArg;
            outFps = inFpsArg * interpFactorArg;
            outMode = outModeArg;
            model = modelArg;

            alpha = false;

            try
            {
                tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
                framesFolder = Path.Combine(tempFolder, Paths.framesDir);
                interpFolder = Path.Combine(tempFolder, Paths.interpDir);
                inputIsFrames = IOUtils.IsPathDirectory(inPath);
                outFilename = Path.Combine(outPath, Path.GetFileNameWithoutExtension(inPath) + IOUtils.GetExportSuffix(interpFactor, ai, model) + FFmpegUtils.GetExt(outMode));
            }
            catch
            {
                Logger.Log("Tried to create InterpSettings struct without an inpath. Can't set tempFolder, framesFolder and interpFolder.", true);
                tempFolder = "";
                framesFolder = "";
                interpFolder = "";
                inputIsFrames = false;
                outFilename = "";
            }

            inputResolution = new Size(0, 0);
            scaledResolution = new Size(0, 0);
        }

        public void UpdatePaths (string inPathArg, string outPathArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
            framesFolder = Path.Combine(tempFolder, Paths.framesDir);
            interpFolder = Path.Combine(tempFolder, Paths.interpDir);
            inputIsFrames = IOUtils.IsPathDirectory(inPath);
            outFilename = Path.Combine(outPath, Path.GetFileNameWithoutExtension(inPath) + IOUtils.GetExportSuffix(interpFactor, ai, model) + FFmpegUtils.GetExt(outMode));
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
                inputResolution = await IOUtils.GetVideoOrFramesRes(inPath);
                scaledResolution = InterpolateUtils.GetOutputResolution(inputResolution, false);
            }
        }

        public int GetTargetFrameCount(string overrideInputDir = "", int overrideFactor = -1)
        {
            if (framesFolder == null || !Directory.Exists(framesFolder))
                return 0;

            string framesDir = (!string.IsNullOrWhiteSpace(overrideInputDir)) ? overrideInputDir : framesFolder;
            int frames = IOUtils.GetAmountOfFiles(framesDir, false, "*.png");
            int factor = (overrideFactor > 0) ? overrideFactor : interpFactor;
            int targetFrameCount = (frames * factor) - (interpFactor - 1);
            return targetFrameCount;
        }
    }
}
