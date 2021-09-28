using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using I = Flowframes.Interpolate;
using Newtonsoft.Json;

namespace Flowframes.Main
{
    class AutoEncodeResume
    {
        public static List<string> processedInputFrames = new List<string>();
        public static int encodedChunks = 0;
        public static int encodedFrames = 0;

        public static void Reset ()
        {
            processedInputFrames = new List<string>();
            encodedChunks = 0;
            encodedFrames = 0;
        }

        public static void Save ()
        {
            string saveDir = Path.Combine(I.current.tempFolder, Paths.resumeDir);
            Directory.CreateDirectory(saveDir);
            string chunksJsonPath = Path.Combine(saveDir, "chunks.json");
            Dictionary<string, string> saveData = new Dictionary<string, string>();
            saveData.Add("encodedChunks", encodedChunks.ToString());
            saveData.Add("encodedFrames", encodedFrames.ToString());
            File.WriteAllText(chunksJsonPath, JsonConvert.SerializeObject(saveData, Formatting.Indented));

            string inputFramesJsonPath = Path.Combine(saveDir, "input-frames.json");
            File.WriteAllText(inputFramesJsonPath, JsonConvert.SerializeObject(processedInputFrames, Formatting.Indented));
        }
    }
}
