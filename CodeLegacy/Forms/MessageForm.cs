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
        private MessageBoxButtons _btns;

        private bool _dialogResultSet = false;

        public MessageForm(string text, string title, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            _text = text;
            _title = title;
            _btns = buttons;

            InitializeComponent();
        }

        private void MessageForm_Load(object sender, EventArgs e)
        {
            Text = _title;
            textLabel.Text = _text;

            if(_btns == MessageBoxButtons.OK)
            {
                SetButtons(true, false, false);
                btn1.Text = "OK";
                AcceptButton = btn1;
            }
            else if(_btns == MessageBoxButtons.YesNo)
            {
                SetButtons(true, true, false);
                btn1.Text = "No";
                btn2.Text = "Yes";
                AcceptButton = btn2;
                CancelButton = btn1;
            }
            else if (_btns == MessageBoxButtons.YesNoCancel)
            {
                SetButtons(true, true, true);
                btn1.Text = "Cancel";
                btn2.Text = "No";
                btn3.Text = "Yes";
                AcceptButton = btn3;
                CancelButton = btn1;
            }

            Size labelSize = GetLabelSize(textLabel);
            Size = new Size((labelSize.Width + 60).Clamp(360, Program.mainForm.Size.Width), (labelSize.Height + 120).Clamp(200, Program.mainForm.Size.Height));

            CenterToScreen();
        }

        private Size GetLabelSize(Label label)
        {
            return TextRenderer.MeasureText(label.Text, label.Font, label.ClientSize, TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        }

        private void SetButtons(bool b1, bool b2, bool b3)
        {
            btn1.Visible = b1;
            btn2.Visible = b2;
            btn3.Visible = b3;
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            if (_btns == MessageBoxButtons.OK) // OK Button
                DialogResult = DialogResult.OK;
            else if (_btns == MessageBoxButtons.YesNo) // No Button
                DialogResult = DialogResult.No;
            else if (_btns == MessageBoxButtons.YesNoCancel) // Cancel Button
                DialogResult = DialogResult.Cancel;

            _dialogResultSet = true;
            Close();
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            if (_btns == MessageBoxButtons.YesNo) // Yes Button
                DialogResult = DialogResult.Yes;
            else if (_btns == MessageBoxButtons.YesNoCancel) // No Button
                DialogResult = DialogResult.No;

            _dialogResultSet = true;
            Close();
        }

        private void btn3_Click(object sender, EventArgs e)
        {
            if (_btns == MessageBoxButtons.YesNoCancel) // Yes Button
                DialogResult = DialogResult.Yes;

            _dialogResultSet = true;
            Close();
        }

        private void MessageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_btns != MessageBoxButtons.OK && !_dialogResultSet)
                e.Cancel = true;
        }
    }
}
