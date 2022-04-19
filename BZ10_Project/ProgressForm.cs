using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BZ10
{
    public partial class ProgressForm : Form
    {
        public ProgressForm(int min, int max)
        {
            InitializeComponent();
            this.progressBar1.Minimum = min;
            this.progressBar1.Maximum = max;
        }
        public void AddProgress()
        {
            progressBar1.Value++;
            //label1.Text = progressBar1.Value.ToString() + "%";
            //label1.Refresh();
        }
        private void ProgressForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
