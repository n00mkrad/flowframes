using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class MessageForm : Form
    {
        private string _text = "";
        private string _title = "";

        public MessageForm(string text, string title)
        {
            _text = text;
            _title = title;
            InitializeComponent();
        }

        private void MessageForm_Load(object sender, EventArgs e)
        {
            Text = _title;
            textLabel.Text = _text;
            Size labelSize = GetLabelSize(textLabel);
            Size = new Size((labelSize.Width + 60).Clamp(360, Program.mainForm.Size.Width), (labelSize.Height + 120).Clamp(200, Program.mainForm.Size.Height));
            CenterToScreen();
        }

        private Size GetLabelSize(Label label)
        {
            return TextRenderer.MeasureText(label.Text, label.Font, label.ClientSize, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
