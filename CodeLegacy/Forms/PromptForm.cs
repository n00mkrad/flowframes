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
    public partial class PromptForm : Form
    {
        public string EnteredText { get; set; }

        public PromptForm(string title, string message, string defaultText)
        {
            InitializeComponent();
            Text = title;
            msgLabel.Text = message;
            textBox.Text = defaultText;
            AcceptButton = confirmBtn;
        }

        private void PromptForm_Load(object sender, EventArgs e)
        {

        }

        private void confirmBtn_Click(object sender, EventArgs e)
        {
            EnteredText = textBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
            Program.mainForm.BringToFront();
        }
    }
}
