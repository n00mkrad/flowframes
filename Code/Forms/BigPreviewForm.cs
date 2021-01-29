﻿using Flowframes.Main;
using System;
using System.Drawing;
using System.Windows.Forms;

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

        public void SetImage(Image img)
        {
            picBox.Image = img;
        }

        private void BigPreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            InterpolateUtils.bigPreviewForm = null;
        }
    }
}
