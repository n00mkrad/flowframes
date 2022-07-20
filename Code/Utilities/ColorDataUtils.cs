using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Utilities
{
    class ColorDataUtils
    {
        // public static async Task<VideoColorData> GetColorData(string path)
        // {
        //     VideoColorData data = new VideoColorData();
        // 
        //     AvProcess.FfprobeSettings settings = new AvProcess.FfprobeSettings() { Args = $"-show_frames -select_streams v:0 -read_intervals \"%+#1\" {path.Wrap()}", LogLevel = "quiet" };
        //     string infoFfprobe = await AvProcess.RunFfprobe(settings);
        // 
        //     string[] linesFfprobe = infoFfprobe.SplitIntoLines();
        // 
        //     foreach (string line in linesFfprobe)
        //     {
        //         if (line.Contains("color_transfer="))
        //             data.ColorTransfer = GetColorTransfer(line.Split('=').Last());
        // 
        //         else if (line.Contains("color_space="))
        //             data.ColorMatrixCoeffs = GetMatrixCoeffs(line.Split('=').Last());
        // 
        //         else if (line.Contains("color_primaries="))
        //             data.ColorPrimaries = GetColorPrimaries(line.Split('=').Last());
        // 
        //         else if (line.Contains("color_range="))
        //             data.ColorRange = GetColorRange(line.Split('=').Last());
        // 
        //         else if (line.Contains("red_x="))
        //             data.RedX = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("red_y="))
        //             data.RedY = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("green_x="))
        //             data.GreenX = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("green_y="))
        //             data.GreenY = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("blue_x="))
        //             data.BlueY = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("blue_y="))
        //             data.BlueX = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("white_point_x="))
        //             data.WhiteY = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("white_point_y="))
        //             data.WhiteX = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("max_luminance="))
        //             data.LumaMax = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("min_luminance="))
        //             data.LumaMin = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("max_content="))
        //             data.MaxCll = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        // 
        //         else if (line.Contains("max_average="))
        //             data.MaxFall = line.Contains("/") ? FractionToFloat(line.Split('=').Last()) : line.Split('=').Last();
        //     }
        // 
        //     string infoMkvinfo = await AvProcess.RunMkvInfo($"{path.Wrap()}", OS.NmkoderProcess.ProcessType.Secondary);
        // 
        //     if (infoMkvinfo.Contains("+ Video track"))
        //     {
        //         string[] lines = infoMkvinfo.Split("+ Video track")[1].Split("+ Track")[0].Split("+ Tags")[0].SplitIntoLines();
        // 
        //         foreach (string line in lines)
        //         {
        //             if (line.Contains("+ Colour transfer:"))
        //                 data.ColorTransfer = ValidateNumber(line.Split(':')[1]).GetInt();
        // 
        //             else if (line.Contains("+ Colour matrix coefficients:"))
        //                 data.ColorMatrixCoeffs = ValidateNumber(line.Split(':')[1]).GetInt();
        // 
        //             else if (line.Contains("+ Colour primaries:"))
        //                 data.ColorPrimaries = ValidateNumber(line.Split(':')[1]).GetInt();
        // 
        //             else if (line.Contains("+ Colour range:"))
        //                 data.ColorRange = ValidateNumber(line.Split(':')[1]).GetInt();
        // 
        //             else if (line.Contains("+ Red colour coordinate x:"))
        //                 data.RedX = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Red colour coordinate y:"))
        //                 data.RedY = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Green colour coordinate x:"))
        //                 data.GreenX = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Green colour coordinate y:"))
        //                 data.GreenY = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Blue colour coordinate y:"))
        //                 data.BlueY = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Blue colour coordinate x:"))
        //                 data.BlueX = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ White colour coordinate y:"))
        //                 data.WhiteY = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ White colour coordinate x:"))
        //                 data.WhiteX = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Maximum luminance:"))
        //                 data.LumaMax = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Minimum luminance:"))
        //                 data.LumaMin = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Maximum content light:"))
        //                 data.MaxCll = ValidateNumber(line.Split(':')[1]);
        // 
        //             else if (line.Contains("+ Maximum frame light:"))
        //                 data.MaxFall = ValidateNumber(line.Split(':')[1]);
        //         }
        //     }
        // 
        //     return data;
        // }

        private static string FractionToFloat(string fracString)
        {
            string[] fracNums = fracString.Split('/');
            return ((float)fracNums[0].GetInt() / (float)fracNums[1].GetInt()).ToString("0.#######", new CultureInfo("en-US"));
        }

        private static string ValidateNumber(string numStr)
        {
            return Double.Parse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture).ToString("0.#######", new CultureInfo("en-US"));
        }

        // public static async Task SetColorData(string path, VideoColorData d)
        // {
        //     try
        //     {
        //         string tmpPath = IoUtils.FilenameSuffix(path, ".tmp");
        // 
        //         List<string> args = new List<string>();
        // 
        //         args.Add($"-o {tmpPath.Wrap()}");
        //         args.Add($"--colour-matrix 0:{d.ColorMatrixCoeffs}");
        //         args.Add($"--colour-transfer-characteristics 0:{d.ColorTransfer}");
        //         args.Add($"--colour-primaries 0:{d.ColorPrimaries}");
        //         args.Add($"--colour-range 0:{d.ColorRange}");
        //         if (!string.IsNullOrWhiteSpace(d.LumaMax)) args.Add($"--max-luminance 0:{d.LumaMax}");
        //         if (!string.IsNullOrWhiteSpace(d.LumaMin)) args.Add($"--min-luminance 0:{d.LumaMin}");
        //         if (!string.IsNullOrWhiteSpace(d.RedX)) args.Add($"--chromaticity-coordinates 0:{d.RedX},{d.RedY},{d.GreenX},{d.GreenY},{d.BlueX},{d.BlueY}");
        //         if (!string.IsNullOrWhiteSpace(d.RedX)) args.Add($"--white-colour-coordinates 0:{d.WhiteX},{d.WhiteY}");
        //         if (!string.IsNullOrWhiteSpace(d.MaxCll)) args.Add($"--max-content-light 0:{d.MaxCll}");
        //         if (!string.IsNullOrWhiteSpace(d.MaxFall)) args.Add($"--max-frame-light 0:{d.MaxFall}");
        //         args.Add($"{path.Wrap()}");
        // 
        //         await AvProcess.RunMkvMerge(string.Join(" ", args), OS.NmkoderProcess.ProcessType.Primary, true);
        // 
        //         if (!File.Exists(tmpPath))
        //         {
        //             Logger.Log($"Error: Muxing failed.");
        //             return;
        //         }
        // 
        //         int filesizeDiffKb = (int)((Math.Abs(new FileInfo(path).Length - new FileInfo(tmpPath).Length)) / 1024);
        //         double filesizeFactor = (double)(new FileInfo(tmpPath).Length) / (double)(new FileInfo(path).Length);
        //         Logger.Log($"{MethodBase.GetCurrentMethod().DeclaringType}: Filesize ratio of remuxed file against original: {filesizeFactor}", true);
        // 
        //         if (filesizeDiffKb > 1024 && (filesizeFactor < 0.95d || filesizeFactor > 1.05d))
        //         {
        //             Logger.Log($"Warning: Output file size differs by >1MB is not within 5% of the original file's size! Won't delete original to be sure.");
        //         }
        //         else
        //         {
        //             File.Delete(path);
        //             File.Move(tmpPath, path);
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Logger.Log($"SetColorData Error: {e.Message}\n{e.StackTrace}");
        //     }
        // }

        public static int GetColorPrimaries(string s) // Defined by the "Color primaries" section of ISO/IEC 23091-4/ITU-T H.273
        {
            s = s.Trim().ToLower();
            if (s == "bt709") return 1;
            if (s == "bt470m") return 4;
            if (s == "bt470bg") return 5;
            if (s == "bt601") return 6;
            if (s == "smpte240m") return 7;
            if (s == "film") return 8;
            if (s == "bt2020") return 9;
            if (s == "smpte428") return 10;
            if (s == "smpte431") return 11;
            if (s == "smpte432") return 12;
            return 2; // Fallback: 2 = Unspecified
        }

        public static int GetColorTransfer(string s) // Defined by the "Transfer characteristics" section of ISO/IEC 23091-4/ITU-T H.273
        {
            s = s.Trim().ToLower();
            if (s == "bt709") return 1;
            if (s == "gamma22" || s == "bt470m") return 4;
            if (s == "gamma28" || s == "bt470bg") return 5; // BT.470 System B, G (historical)
            if (s == "bt601" || s == "smpte170m") return 6; // BT.601
            if (s == "smpte240m") return 7; // SMPTE 240 M
            if (s == "linear") return 8; // Linear
            //if (s == "?") return 9; // Logarithmic(100 : 1 range)
            //if (s == "?") return 10; // Logarithmic (100 * Sqrt(10) : 1 range)
            if (s == "iec61966-2-4") return 11; // IEC 61966-2-4
            if (s == "bt1361" || s == "bt1361e") return 12; // BT.1361
            if (s == "srgb") return 13; // SRGB
            if (s == "bt2020-10") return 14; // BT.2020 10-bit systems
            if (s == "bt2020-12") return 15; // BT.2020 12-bit systems
            if (s == "smpte2084") return 16; // SMPTE ST 2084, ITU BT.2100 PQ
            if (s == "smpte428") return 17; // SMPTE ST 428
            if (s == "bt2100") return 18; // BT.2100 HLG, ARIB STD-B67
            return 2; // Fallback: 2 = Unspecified
        }

        public static int GetMatrixCoeffs(string s) // Defined by the "Matrix coefficients" section of ISO/IEC 23091-4/ITU-T H.27
        {
            s = s.Trim().ToLower();
            if (s == "bt709") return 1;
            if (s == "fcc") return 4; // US FCC 73.628
            if (s == "bt470bg") return 5; // BT.470 System B, G (historical)
            if (s == "bt601" || s == "smpte170m") return 6; // BT.601
            if (s == "smpte240m") return 7; // SMPTE 240 M
            if (s == "ycgco") return 8; // YCgCo
            if (s == "bt2020ncl" || s == "bt2020nc") return 9; // BT.2020 non-constant luminance, BT.2100 YCbCr
            if (s == "bt2020") return 10; // BT.2020 constant luminance
            if (s == "smpte2085") return 11; // SMPTE ST 2085 YDzDx
            // 12: MC_CHROMAT_NCL - Chromaticity-derived non-constant luminance
            // 13: MC_CHROMAT_CL - Chromaticity-derived constant luminance
            // 14: MC_ICTCP BT.2100 - ICtCp
            return 2; // Fallback: 2 = Unspecified
        }

        public static int GetColorRange(string s) // Defined by the "Matrix coefficients" section of ISO/IEC 23091-4/ITU-T H.27
        {
            s = s.Trim().ToLower();
            if (s == "tv") return 1; // TV
            if (s == "pc") return 2; // PC/Full
            return 0; // Fallback: Unspecified
        }

        public static string FormatForAom(string colorspace)
        {
            return colorspace.Replace("bt2020-10", "bt2020-10bit").Replace("bt2020-12", "bt2020-12bit");
        }

        #region Get string from int

        public static string GetColorPrimariesString(int n)
        {
            switch (n)
            {
                case 1: return "bt709";
                case 4: return "bt470m";
                case 5: return "bt470bg";
                case 6: return "bt601";
                case 7: return "smpte240m";
                case 8: return "film";
                case 9: return "bt2020";
                case 10: return "smpte428";
                case 11: return "smpte431";
                case 12: return "smpte432";
            }

            return "";
        }

        public static string GetColorTransferString(int n)
        {
            switch (n)
            {
                case 1: return "bt709";
                case 4: return "gamma22"; // "bt470m"
                case 5: return "gamma28"; // "bt470bg"
                case 6: return "bt601"; // "smpte170m"
                case 7: return "smpte240m";
                case 8: return "linear";
                case 11: return "iec61966-2-4";
                case 12: return "bt1361";
                case 13: return "srgb";
                case 14: return "bt2020-10";
                case 15: return "bt2020-12";
                case 16: return "smpte2084";
                case 17: return "smpte428";
                case 18: return "bt2100";
            }

            return "";
        }

        public static string GetColorMatrixCoeffsString(int n)
        {
            switch (n)
            {
                case 1: return "bt709";
                case 4: return "fcc";
                case 5: return "bt470bg";
                case 6: return "bt601";
                case 7: return "smpte240m";
                case 8: return "ycgco";
                case 9: return "bt2020ncl";
                case 10: return "bt2020";
            }

            return "";
        }

        public static string GetColorRangeString(int n)
        {
            switch (n)
            {
                case 1: return "tv";
                case 2: return "pc";
            }

            return "";
        }

        #endregion

        #region Get friendly name from int

        public static string GetColorPrimariesName(int n)
        {
            switch (n)
            {
                case 1: return "BT.709";
                case 2: return "Unspecified";
                case 4: return "BT.470 System B, G (historical)";
                case 5: return "BT.470 System M (historical)";
                case 6: return "BT.601";
                case 7: return "SMPTE 240";
                case 8: return "Generic film (color filters using illuminant C)";
                case 9: return "BT.2020, BT.2100";
                case 10: return "SMPTE 428 (CIE 1921 XYZ)";
                case 11: return "SMPTE RP 431-2";
                case 12: return "SMPTE EG 432-1";
                case 22: return "EBU Tech. 3213-E";
            }

            return "Unknown";
        }

        public static string GetColorTransferName(int n)
        {
            switch (n)
            {
                case 1: return "BT.709";
                case 2: return "Unspecified";
                case 4: return "BT.470 System B, G (historical)";
                case 5: return "BT.470 System M (historical)";
                case 6: return "BT.601";
                case 7: return "SMPTE 240 M";
                case 8: return "Linear";
                case 9: return "Logarithmic (100 : 1 range)";
                case 10: return "Logarithmic (100 * Sqrt(10) : 1 range)";
                case 11: return "IEC 61966-2-4";
                case 12: return "BT.1361";
                case 13: return "sRGB or sYCC";
                case 14: return "BT.2020 10-bit systems";
                case 15: return "BT.2020 12-bit systems";
                case 16: return "SMPTE ST 2084, ITU BT.2100 PQ";
                case 17: return "SMPTE ST 428";
                case 18: return "BT.2100 HLG, ARIB STD-B67";
            }

            return "Unknown";
        }

        public static string GetColorMatrixCoeffsName(int n)
        {
            switch (n)
            {
                case 1: return "BT.709";
                case 2: return "Unspecified";
                case 4: return "US FCC 73.628";
                case 5: return "BT.470 System B, G (historical)";
                case 6: return "BT.601";
                case 7: return "SMPTE 240 M";
                case 8: return "YCgCo";
                case 9: return "BT.2020 non-constant luminance, BT.2100 YCbCr";
                case 10: return "BT.2020 constant luminance";
                case 11: return "SMPTE ST 2085 YDzDx";
                case 12: return "Chromaticity-derived non-constant luminance";
                case 13: return "Chromaticity-derived constant luminance";
                case 14: return "BT.2100 ICtCp";
            }

            return "Unknown";
        }

        public static string GetColorRangeName(int n)
        {
            switch (n)
            {
                case 0: return "Unspecified";
                case 1: return "TV (Limited)";
                case 2: return "PC (Full)";
            }

            return "Unknown";
        }

        #endregion
    }
}
