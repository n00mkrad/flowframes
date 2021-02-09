using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

		public static bool AssignComboxIndexFromText (ComboBox box, string text)	// Set index to corresponding text
        {
			int index = box.Items.IndexOf(text);

			if (index == -1)    // custom value, index not found
				return false;

			box.SelectedIndex = index;
			return true;
		}

		public static ComboBox FillAiModelsCombox (ComboBox combox, AI ai)
        {
			combox.Items.Clear();

            try
            {
				string pkgPath = PkgUtils.GetPkgFolder(ai.pkg);
				string modelsFile = Path.Combine(pkgPath, "models.txt");
				string[] modelsWithDec = IOUtils.ReadLines(modelsFile);

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
			}
            catch (Exception e)
            {
				Logger.Log($"Failed to load available AI models for {ai.aiName}! {e.Message}");
            }

			return combox;
		}
	}
}
