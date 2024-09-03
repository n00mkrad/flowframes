using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.Os;
using System;
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

        public static ComboBox LoadAiModelsIntoGui(ComboBox combox, AI ai)
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
            if (NvApi.gpuList.Count < 1)
            {
                for (int i = 0; i < combox.Items.Count; i++)
                {
                    if (((string)combox.Items[i]).Upper().Contains("NCNN"))
                        combox.SelectedIndex = i;
                }
            }
        }

        public enum MessageType { Message, Warning, Error };

        public static DialogResult ShowMessageBox(string text, MessageType type = MessageType.Message)
        {
            Logger.Log($"MessageBox: {text} ({type}){(BatchProcessing.busy ? "[Batch Mode - Will not display messagebox]" : "")}", true);

            if (BatchProcessing.busy)
            {
                Logger.Log(text);
                return new DialogResult();
            }

            MessageBoxIcon icon = MessageBoxIcon.Information;
            if (type == MessageType.Warning) icon = MessageBoxIcon.Warning;
            else if (type == MessageType.Error) icon = MessageBoxIcon.Error;

            MessageForm form = new MessageForm(text, type.ToString());
            form.ShowDialog();
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
    }
}
