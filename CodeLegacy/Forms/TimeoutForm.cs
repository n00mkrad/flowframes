using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class TimeoutForm : Form
    {
        string actionName = "";
        int waitSeconds;

        public delegate void ActionCallback();
        public static ActionCallback actionCallback;

        bool cancelCountdown = false;

        public TimeoutForm(string action, ActionCallback callback, int waitSecs = 20, string windowTitle = "Timeout")
        {
            actionName = action;
            Text = windowTitle;
            actionCallback = callback;
            waitSeconds = waitSecs;
            InitializeComponent();
        }

        private void TimeoutForm_Load(object sender, EventArgs e)
        {

        }

        private void TimeoutForm_Shown(object sender, EventArgs e)
        {
            mainLabel.Text = $"Waiting before running action \"{actionName}\"";
            WaitAndRun();
        }

        async Task WaitAndRun ()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();

            for (int i = waitSeconds; i > 0; i--)
            {
                countdownLabel.Text = $"{i}s";
                await Task.Delay(1000);
            }

            if (cancelCountdown)
                return;

            actionCallback();
            Close();
        }

        private void skipCountdownBtn_Click(object sender, EventArgs e)
        {
            cancelCountdown = true;
            actionCallback();
            Close();
        }

        private void cancelActionBtn_Click(object sender, EventArgs e)
        {
            cancelCountdown = true;
            Close();
        }

        
    }
}
