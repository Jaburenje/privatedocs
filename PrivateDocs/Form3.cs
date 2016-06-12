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
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text!="")
            {
                FormsVar.Password = System.Text.Encoding.Unicode.GetBytes(textBox1.Text);
				this.DialogResult=DialogResult.OK;
                this.Close();
            }
            else
                System.Windows.Forms.MessageBox.Show("Вы не ввели пароль!");
        }
    }
}
