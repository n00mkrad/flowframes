using System.Drawing;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class SplashForm : Form
    {
        public enum TextSize { Small, Medium, Large }
        private static readonly string[] fontPresets = { "Yu Gothic UI, 14pt", "Yu Gothic UI, 18pt", "Yu Gothic UI, 21.75pt" };

        public SplashForm(string status = "", bool topMost = true, TextSize textSize = TextSize.Large, bool show = true)
        {
            InitializeComponent();
            SetStatus(status);
            TopMost = topMost;
            statusLabel.Font = (Font)new FontConverter().ConvertFromInvariantString(fontPresets[(int)textSize]);

            if (show)
            {
                Show();
            }
        }

        private void SplashForm_Load(object sender, System.EventArgs e)
        {
            if (!Program.CmdMode)
            {
                Opacity = 1f;
            }
        }

        public void SetStatus(string status)
        {
            statusLabel.Text = status;
        }
    }
}
