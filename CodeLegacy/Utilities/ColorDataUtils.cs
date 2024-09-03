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
        public static int GetColorPrimaries(string s) // Defined by the "Color primaries" section of ISO/IEC 23091-4/ITU-T H.273
        {
            s = s.Trim().Lower();
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
            s = s.Trim().Lower();
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
            s = s.Trim().Lower();
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
            s = s.Trim().Lower();
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
