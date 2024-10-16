using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class SplashForm : Form
    {
        public static SplashForm Inst;

        public SplashForm(string status = "")
        {
            Inst = this;
            InitializeComponent();
            SetStatus(status);
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
