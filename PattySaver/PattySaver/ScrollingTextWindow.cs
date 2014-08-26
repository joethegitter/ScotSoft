using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.LaunchManager;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver
{
    public partial class ScrollingTextWindow : Form
    {
        public ScrollingTextWindow()
        {
            InitializeComponent();
            textBox1.Text = Logging.strBuffer;
            textBox1.Select(textBox1.Text.Length - 1, 0);
        }

        /// <summary>
        /// Override of ShowWithoutActivation property.  In theory, this tells
        /// Windows that when our window gets shown, it should not receive activation.
        /// </summary>
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
    }


}
