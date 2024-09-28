using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class SplashForm : Form
    {
        public SplashForm()
        {
            InitializeComponent();
        }

        private void SplashForm_Load(object sender, System.EventArgs e)
        {
            if (!Program.CmdMode)
            {
                Opacity = 1f;
            }
        }
    }
}
