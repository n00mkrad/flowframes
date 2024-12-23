using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.MiscUtils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enc = Flowframes.Data.Enums.Encoding.Encoder;

namespace Flowframes.Media
{
    internal class HwEncCheck
    {
        public static async Task DetectHwEncoders(bool hideMainForm = true, bool onlyShowAvail = true, int waitMs = 0)
        {
            if (Config.GetBool(Config.Key.PerformedHwEncCheck))
                return;

            var statusForm = new SplashForm("Checking hardware encoders...", textSize: SplashForm.TextSize.Medium);
            var previousMainFormOpacity = Program.mainForm.Opacity;

            if (hideMainForm)
            {
                Program.mainForm.Opacity = 0;
            }

            try
            {
                var encoders = new[] { Enc.Nvenc264, Enc.Nvenc265, Enc.NvencAv1, Enc.Qsv264, Enc.Qsv265, Enc.Amf264, Enc.Amf265 };
                var encoderNames = encoders.Select(x => OutputUtils.GetEncoderInfoVideo(x).Name);
                var compatEncoders = new List<string>();

                foreach (string e in encoderNames)
                {
                    bool compat = await FfmpegCommands.IsEncoderCompatible(e);

                    if (compat)
                    {
                        compatEncoders.Add(e);
                        Logger.Log($"HW Encoder supported: {e}", true);
                    }

                    if(!compat && onlyShowAvail)
                        continue;

                    statusForm.SetStatus($"Checking hardware encoders...\n{(compat ? "Available" : "Not available")}: {e.Replace("_", " ").Upper()}");
                }

                var compEncsPretty = compatEncoders.Select(e => e.Replace("_", " ").Upper());
                
                if(waitMs > 0)
                {
                    string availNvidia = string.Join(", ", compEncsPretty.Where(e => e.Contains("NVENC")).Select(e => e.Replace("NVENC", "").Trim())) + "\n";
                    string availAmd = string.Join(", ", compEncsPretty.Where(e => e.Contains("AMF")).Select(e => e.Replace("AMF", "").Trim())) + "\n";
                    string availIntel = string.Join(", ", compEncsPretty.Where(e => e.Contains("QSV")).Select(e => e.Replace("QSV", "").Trim())) + "\n";
                    if (availNvidia.IsEmpty()) availNvidia = "None";
                    if (availAmd.IsEmpty()) availAmd = "None";
                    if (availIntel.IsEmpty()) availIntel = "None";
                    statusForm.SetStatus($"Nvidia NVENC: {availNvidia}AMD AMF: {availAmd}Intel QuickSync: {availIntel}".Trim());
                    await Task.Delay(waitMs);
                }

                if(compEncsPretty.Any())
                {
                    Logger.Log($"Available hardware encoders: {string.Join(", ", compEncsPretty)}");
                }

                Config.Set(Config.Key.SupportedHwEncoders, string.Join(",", compatEncoders));
                Config.Set(Config.Key.PerformedHwEncCheck, true.ToString());
            }
            catch { }

            statusForm.Close();
            Program.mainForm.Opacity = previousMainFormOpacity;
        }
    }
}
