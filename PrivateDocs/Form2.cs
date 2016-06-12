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
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            Form1 main = this.Owner as Form1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((textBox1.Text != "") && (textBox2.Text != ""))
            {
                FormsVar.CSize = Convert.ToInt32(textBox1.Text);
                string s = textBox2.Text;
                byte[] test = System.Text.Encoding.Unicode.GetBytes(textBox2.Text);
                FormsVar.Password = test;
                this.DialogResult = DialogResult.OK;
            }
            else
                System.Windows.Forms.MessageBox.Show("Вы не ввели пароль или размер контейнера!");
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            ToolTip1.SetToolTip(this.textBox1, "Максимальный размер равен 16384МБ, минимальный 128 МБ");
            System.Windows.Forms.ToolTip ToolTip2 = new System.Windows.Forms.ToolTip();
            ToolTip2.SetToolTip(this.textBox2, "Максимальная длина пароля - 16 символов");
        }
    }
}
