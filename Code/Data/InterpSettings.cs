using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using System;
using System.Collections.Generic;
using System.IO;

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
        public int tilesize;

        public string tempFolder;
        public string framesFolder;
        public string interpFolder;
        public bool inputIsFrames;
        public string outFilename;

        public InterpSettings(string inPathArg, string outPathArg, AI aiArg, float inFpsArg, int interpFactorArg, Interpolate.OutMode outModeArg, int tilesizeArg = 512)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            ai = aiArg;
            inFps = inFpsArg;
            interpFactor = interpFactorArg;
            outFps = inFpsArg * interpFactorArg;
            outMode = outModeArg;
            tilesize = tilesizeArg;

            tempFolder = InterpolateUtils.GetTempFolderLoc(inPath, outPath);
            framesFolder = Path.Combine(tempFolder, Paths.framesDir);
            interpFolder = Path.Combine(tempFolder, Paths.interpDir);

            inputIsFrames = IOUtils.IsPathDirectory(inPath);
            outFilename = Path.Combine(outPath, Path.GetFileNameWithoutExtension(inPath) + IOUtils.GetAiSuffix(ai, interpFactor) + InterpolateUtils.GetExt(outMode));
        }

        public void SetFps(float inFpsArg)
        {
            inFps = inFpsArg;
            outFps = inFps * interpFactor;
        }
    }
}
