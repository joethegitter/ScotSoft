using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Imaging;
using System.IO;
using System.Reflection;


namespace ScotSoft.PattySaver
{
    public partial class HelpAbout : Form
    {
        private bool OpenedFromFullScreenForm;
        private Form callingForm;

        public HelpAbout()
        {
            InitializeComponent();
        }

        public HelpAbout(bool LaunchedFromFullScreen, Form parentForm)
        {
            InitializeComponent();

            OpenedFromFullScreenForm = true;
            callingForm = parentForm;
            if (parentForm.WindowState == FormWindowState.Maximized) this.TopMost = true;
        }

        private void HelpAbout_Load(object sender, EventArgs e)
        {
            // Set the titlebar
            this.Text = ProductName;

            // pbTwoGuys picture box is docked, so it's height is fixed (height of the form).
            // pbTwoGuys.SizeMode = Zoom, so it's going to show the picture at the fixed height, with
            // black space on either side of the picture. So let's resize the width of the picturebox
            // to match the picture, and therefore show no whitespace.

            // get the dimensions of the image, so we can figure out ratio and set width of pb accordingly
            float width = pbTwoGuys.Image.PhysicalDimension.Width;
            float height = pbTwoGuys.Image.PhysicalDimension.Height;

            float ratio = width / height;
            float pbHeight = pbTwoGuys.Height;
            float pbWidth = pbHeight * ratio;

            int newPbWidth = Convert.ToInt32(pbWidth);
            pbTwoGuys.Width = newPbWidth;

            pbTwoGuys.SizeMode = PictureBoxSizeMode.Zoom;

            // Set the max width of the keyexplanation label to something short of the width of the scrollable panel it's in
            int newWidth = pnlInner.Width - 30;
            lblKeyExplanation.MaximumSize = new Size(newWidth, int.MaxValue);

            // Set the text of the label
            string preamble = "Welcome to " + ProductName + ", Version: " + ProductVersion + Environment.NewLine + Environment.NewLine;
            lblKeyExplanation.Text = preamble + PattySaverResources.HelpAboutText;

        }
    }
}
