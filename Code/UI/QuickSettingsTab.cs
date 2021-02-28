using Flowframes.Media;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.MiscUtils;

namespace Flowframes.UI
{
    class QuickSettingsTab
    {
        public static bool trimEnabled;
        public static bool doTrimEnd;
        public static string trimStart;
        public static string trimEnd;

        public static void UpdateTrim (TextBox trimStartBox, TextBox trimEndBox)
        {
            trimStart = trimStartBox.Text.Trim();
            trimEnd = trimEndBox.Text.Trim();

            long startSecs = FormatUtils.TimestampToSecs(trimStart, false);
            long endSecs = FormatUtils.TimestampToSecs(trimEnd, false);

            if (endSecs <= startSecs)
                trimEndBox.Text = FormatUtils.SecsToTimestamp(startSecs + 1);

            long dur = FormatUtils.TimestampToMs(trimEnd, false) - FormatUtils.TimestampToMs(trimStart, false);
            Program.mainForm.currInDurationCut = dur;

            doTrimEnd = FormatUtils.TimestampToMs(trimEnd, false) != FormatUtils.TimestampToMs(FormatUtils.MsToTimestamp(Program.mainForm.currInDuration), false);
        }

        public static string GetTrimEndMinusOne ()
        {
            TimeSpan minusOne = TimeSpan.Parse(trimEnd).Subtract(new TimeSpan(0, 0, 1));
            Logger.Log($"returning {minusOne}", true, false, "ffmpeg");
            return minusOne.ToString();
        }
    }
}
