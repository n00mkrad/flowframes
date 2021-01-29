using Flowframes.Data;
using Flowframes.IO;
using System.IO;
using System.Windows.Forms;

namespace Flowframes.UI
{
    class UIUtils
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

        public static ComboBox FillAiModelsCombox(ComboBox combox, AI ai)
        {
            string pkgPath = PkgUtils.GetPkgFolder(ai.pkg);
            string modelsFile = Path.Combine(pkgPath, "models.txt");
            string[] modelsWithDec = IOUtils.ReadLines(modelsFile);
            combox.Items.Clear();

            for (int i = 0; i < modelsWithDec.Length; i++)
            {
                string model = modelsWithDec[i];

                if (string.IsNullOrWhiteSpace(model))
                    continue;

                combox.Items.Add(model);

                if (model.Contains("Recommended") || model.Contains("Default"))
                    combox.SelectedIndex = i;
            }

            if (combox.SelectedIndex < 0)
                combox.SelectedIndex = 0;

            return combox;
        }
    }
}
