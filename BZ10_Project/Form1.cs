using BZ10.Common;
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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text == "admin")
                {
                    DebOutPut.DebLog("进入调试模式");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("密码错误!");
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            if (Param.isSoftKeyBoard == "0")
                SoftKeyboardCtrl.OpenAndSetWindow();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SoftKeyboardCtrl.CloseWindow();
        }
    }
}
