using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.IO
{
    class ConfigParser
    {

		public enum StringMode { Any, Int, Float }

		public static void SaveGuiElement(TextBox textbox, StringMode stringMode = StringMode.Any)
		{
            switch (stringMode)
            {
				case StringMode.Any: Config.Set(textbox.Name, textbox.Text); break;
				case StringMode.Int: Config.Set(textbox.Name, textbox.Text.GetInt().ToString()); break;
				case StringMode.Float: Config.Set(textbox.Name, textbox.Text.GetFloat().ToString()); break;
			}
		}

		public static void SaveGuiElement(ComboBox comboBox, StringMode stringMode = StringMode.Any)
		{
			switch (stringMode)
			{
				case StringMode.Any: Config.Set(comboBox.Name, comboBox.Text); break;
				case StringMode.Int: Config.Set(comboBox.Name, comboBox.Text.GetInt().ToString()); break;
				case StringMode.Float: Config.Set(comboBox.Name, comboBox.Text.GetFloat().ToString()); break;
			}
		}

		public static void SaveGuiElement(CheckBox checkbox)
		{
			Config.Set(checkbox.Name, checkbox.Checked.ToString());
		}

		public static void SaveComboxIndex(ComboBox comboBox)
		{
			Config.Set(comboBox.Name, comboBox.SelectedIndex.ToString());
		}

		public static void LoadGuiElement(ComboBox comboBox, string suffix = "")
		{
			comboBox.Text = Config.Get(comboBox.Name) + suffix;
		}

		public static void LoadGuiElement(TextBox textbox, string suffix = "")
		{
			textbox.Text = Config.Get(textbox.Name) + suffix; ;
		}

		public static void LoadGuiElement(CheckBox checkbox)
		{
			checkbox.Checked = Config.GetBool(checkbox.Name);
		}

		public static void LoadComboxIndex(ComboBox comboBox)
		{
			comboBox.SelectedIndex = Config.GetInt(comboBox.Name);
		}
	}
}
