using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.Main;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Flowframes.Ui
{
    class UiUtils
    {
        public static void InitCombox(ComboBox box, int index)
        {
            if (box.Items.Count >= 1)
            {
                box.SelectedIndex = index;
                box.Text = box.Items[index].ToString();
            }
        }

        public static bool AssignComboxIndexFromText(ComboBox box, string text)	// Set index to corresponding text
        {
            int index = box.Items.IndexOf(text);

            if (index == -1)    // custom value, index not found
                return false;

            box.SelectedIndex = index;
            return true;
        }

        public static ComboBox LoadAiModelsIntoGui(ComboBox combox, AiInfo ai)
        {
            combox.Items.Clear();

            try
            {
                ModelCollection modelCollection = AiModels.GetModels(ai);

                if (modelCollection.Models == null || modelCollection.Models.Count < 1)
                    return combox;

                for (int i = 0; i < modelCollection.Models.Count; i++)
                {
                    ModelCollection.ModelInfo modelInfo = modelCollection.Models[i];

                    if (string.IsNullOrWhiteSpace(modelInfo.Name))
                        continue;

                    combox.Items.Add(modelInfo.GetUiString());

                    if (modelInfo.IsDefault)
                        combox.SelectedIndex = i;
                }

                if (combox.SelectedIndex < 0)
                    combox.SelectedIndex = 0;

                SelectNcnnIfNoCudaAvail(combox);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to load available AI models for {ai.NameInternal}! {e.Message}");
                Logger.Log($"Stack Trace: {e.StackTrace}", true);
            }

            return combox;
        }

        static void SelectNcnnIfNoCudaAvail(ComboBox combox)
        {
            if (NvApi.NvGpus.Count < 1)
            {
                for (int i = 0; i < combox.Items.Count; i++)
                {
                    if (((string)combox.Items[i]).Upper().Contains("NCNN"))
                        combox.SelectedIndex = i;
                }
            }
        }

        public enum MessageType { Message, Warning, Error };

        public static DialogResult ShowMessageBox(string text, MessageType type = MessageType.Message, bool monospace = false)
        {
            Logger.Log($"MessageBox: {text} ({type}){(BatchProcessing.busy ? "[Batch Mode - Will not display messagebox]" : "")}", true);

            if(Program.CmdMode)
                return DialogResult.OK;

            if (BatchProcessing.busy)
            {
                Logger.Log(text);
                return new DialogResult();
            }

            MessageBoxIcon icon = MessageBoxIcon.Information;
            if (type == MessageType.Warning) icon = MessageBoxIcon.Warning;
            else if (type == MessageType.Error) icon = MessageBoxIcon.Error;

            var msgForm = new MessageForm(text, type.ToString(), monospace: monospace) { TopMost = true };
            Program.mainForm.Invoke(() => msgForm.ShowDialog());
            return DialogResult.OK;
        }

        public static DialogResult ShowMessageBox(string text, string title, MessageBoxButtons btns)
        {
            MessageForm form = new MessageForm(text, title, btns);
            return form.ShowDialog();
        }
         
        public enum MoveDirection { Up = -1, Down = 1 };

        public static void MoveListViewItem(ListView listView, MoveDirection direction)
        {
            if (listView.SelectedItems.Count != 1)
                return;

            ListViewItem selected = listView.SelectedItems[0];
            int index = selected.Index;
            int count = listView.Items.Count;

            if (direction == MoveDirection.Up)
            {
                if (index == 0)
                {
                    listView.Items.Remove(selected);
                    listView.Items.Insert(count - 1, selected);
                }
                else
                {
                    listView.Items.Remove(selected);
                    listView.Items.Insert(index - 1, selected);
                }
            }
            else
            {
                if (index == count - 1)
                {
                    listView.Items.Remove(selected);
                    listView.Items.Insert(0, selected);
                }
                else
                {
                    listView.Items.Remove(selected);
                    listView.Items.Insert(index + 1, selected);
                }
            }
        }

        // TODO: Move to NmkdUtils once implemented
        public static string PascalCaseToText(string s)
        {
            if (s.IsEmpty())
                return "";

            return Regex.Replace(s, "([A-Z])", " $1").Trim();
        }

        private static Dictionary<string, bool> _fontAvailabilityCache = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary> Checks if a <paramref name="font"/> is installed on the system, results are cached. </summary>
        public static bool IsFontInstalled(string font)
        {
            if (_fontAvailabilityCache.TryGetValue(font, out bool isInstalledCached))
                return isInstalledCached;

            using (var testFont = new System.Drawing.Font(font, 8))
            {
                bool isInstalled = string.Equals(font, testFont.Name, StringComparison.InvariantCultureIgnoreCase);
                _fontAvailabilityCache[font] = isInstalled;
                return isInstalled;
            }
        }

        /// <summary> Gets a font or tries one of the fallbacks (if provided). If none are found, returns the default font. </summary>
        public static System.Drawing.Font GetFontOrFallback(string preferredFont, float emSize, System.Drawing.FontStyle? style = null, params string[] fallbackFonts)
        {
            style = style ?? System.Drawing.FontStyle.Regular;
            if (IsFontInstalled(preferredFont))
                return new System.Drawing.Font(preferredFont, emSize, style.Value);
            foreach (var fallback in fallbackFonts)
            {
                if (IsFontInstalled(fallback))
                    return new System.Drawing.Font(fallback, emSize, style.Value);
            }
            // Last resort: use default font
            return new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, emSize, style.Value);
        }

        public static System.Drawing.Font GetMonospaceFont(float emSize, System.Drawing.FontStyle? style = null)
            => GetFontOrFallback("Cascadia Mono", emSize, style ?? System.Drawing.FontStyle.Regular, "Cascadia Code", "Consolas", "Courier New");
    }
}
