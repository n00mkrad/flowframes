using Flowframes.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.Ui;

namespace Flowframes.Forms
{
    public partial class BigPreviewForm : Form
    {
        public BigPreviewForm()
        {
            InitializeComponent();
        }

        private void BigPreviewForm_Load(object sender, EventArgs e)
        {

        }

        public void SetImage (Image img)
        {
            picBox.Image = img;
        }

        private void BigPreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            InterpolationProgress.bigPreviewForm = null;
        }
    }
}
