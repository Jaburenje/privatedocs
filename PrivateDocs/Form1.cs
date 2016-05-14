using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrivateDocs
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Test test = new Test(Convert.ToInt32(textBox1.Text)*1024*1024,@"C:\111");
            test.WritePrimaryBlock(test.Size);
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Test test = new Test(1, textBox2.Text);
            test.ReadContainer(test.Path);
        }
    }
}
