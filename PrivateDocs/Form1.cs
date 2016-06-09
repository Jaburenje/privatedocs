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
            //Test test = new Test(1,"C:\\");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Test test = new Test(Convert.ToInt32(textBox1.Text)*1024*1024,@"C:\111");
            saveFileDialog1.InitialDirectory = "c:\\";
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"  ;
            saveFileDialog1.FilterIndex = 2 ;
            saveFileDialog1.RestoreDirectory = true ;
            Form2 containerForm = new Form2();
            containerForm.Owner = this;
            if (containerForm.ShowDialog()==DialogResult.OK)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    test = new Test(FormsVar.CSize * 1024 * 1024, saveFileDialog1.FileName);
                    test.WritePrimaryBlock(test.Size);
                   
                }
            }
            GC.Collect();            
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            //Test test = new Test(1, textBox2.Text);
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Form3 passForm = new Form3();
                passForm.Owner = this;
                passForm.ShowDialog();
                test = new Test(1, openFileDialog1.FileName);
                if (FormsVar.Password!=null)
                if (test.OpenContainer(openFileDialog1.FileName, System.Text.Encoding.Unicode.GetString(FormsVar.Password)))
                {
                    label1.Text = FormsVar.BSize.ToString()+ " Блоков по 4 кб";
                    if (FormsVar.BSize > 1048576)
                    {
                        var cnt = (FormsVar.BSize*4) / 1024 ;
                        label2.Text = Convert.ToString(cnt) + " MB";
                    }
                    else
                    if (FormsVar.BSize > 256)
                    {
                        var cnt = (FormsVar.BSize * 4096) / 1024 / 1024;
                        label2.Text = Convert.ToString(cnt) + " MB";
                    }
                    else
                    {
                        var cnt = (FormsVar.BSize * 4096) / 1024;
                        label2.Text = Convert.ToString(cnt) + " KB";
                    }
                    List<string> list = new List<string>();
                    list = test.ReadFiles();
                    for (var i = 0; i < list.Count; i++)
                        checkedListBox1.Items.Add(list[i]);
                }
            }
            
            GC.Collect();            
                //listBox1.Items.Add(list[i]);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Test test = new Test(1, textBox2.Text);
            if (FormsVar.Password!=null)
            {
                test.ReadContainer(test.Path);
                openFileDialog1.InitialDirectory = "E:\\теория";
                openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    test.AddFile(openFileDialog1.FileName);
                    test.ReadContainer(test.Path);
                    List<string> list = new List<string>();
                    checkedListBox1.Items.Clear();
                    list = test.ReadFiles();
                    for (var i = 0; i < list.Count; i++)
                        checkedListBox1.Items.Add(list[i]);
                    label1.Text = FormsVar.BSize.ToString() + " Блоков по 4 кб";
                    if (FormsVar.BSize > 1048576)
                    {
                        var cnt = (FormsVar.BSize * 4) / 1024;
                        label2.Text = Convert.ToString(cnt) + " MB";
                    }
                    else
                        if (FormsVar.BSize > 256)
                        {
                            var cnt = (FormsVar.BSize * 4096) / 1024 / 1024;
                            label2.Text = Convert.ToString(cnt) + " MB";
                        }
                        else
                        {
                            var cnt = (FormsVar.BSize * 4096) / 1024;
                            label2.Text = Convert.ToString(cnt) + " KB";
                        }
                }
            }
            GC.Collect();            

                //try
                //{
                //    test.AddFile(openFileDialog1.FileName);  
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                //}
            
            //test.AddFile(@"C:/test.txt");
           

        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
                if( result == DialogResult.OK )
            {
                //Test test = new Test(1, textBox2.Text);
                test.ReadContainer(test.Path);

                foreach (int indexChecked in checkedListBox1.CheckedIndices)
                {
                    string tmp = checkedListBox1.Items[indexChecked].ToString();
                    test.ReadFileFromFS(tmp,folderBrowserDialog1.SelectedPath);
                }
                label1.Text = FormsVar.BSize.ToString() + " Блоков по 4 кб";

                if (FormsVar.BSize > 1048576)
                {
                    var cnt = (FormsVar.BSize * 4) / 1024;
                    label2.Text = Convert.ToString(cnt) + " MB";
                }
                else
                    if (FormsVar.BSize > 256)
                    {
                        var cnt = (FormsVar.BSize * 4096) / 1024 / 1024;
                        label2.Text = Convert.ToString(cnt) + " MB";
                    }
                    else
                    {
                        var cnt = (FormsVar.BSize * 4096) / 1024;
                        label2.Text = Convert.ToString(cnt) + " KB";
                    }

            }
                GC.Collect();            


           //    Test test = new Test(1, textBox2.Text);
           //    test.ReadContainer(test.Path);

           //foreach (int indexChecked in checkedListBox1.CheckedIndices)
           //{
           //    string tmp = checkedListBox1.Items[indexChecked].ToString();
           //    test.ReadFileFromFS(tmp);
           //}
        }

        private void button5_Click(object sender, EventArgs e)
        {
           
                //if (test.ReadContainer(test.Path))
                test.ReadContainer(test.Path);
                    foreach (int indexChecked in checkedListBox1.CheckedIndices)
                    {
                        string tmp = checkedListBox1.Items[indexChecked].ToString();
                        test.RemoveFile(tmp);
                        //if (test.ReadContainer(test.Path))//, System.Text.Encoding.Unicode.GetString(FormsVar.Password)))
                        //{
                                checkedListBox1.Items.Clear();
                                List<string> list = new List<string>();
                                list = test.ReadFiles();
                                for (var i = 0; i < list.Count; i++)
                                    checkedListBox1.Items.Add(list[i]);
                        //}
                    }
                label1.Text = FormsVar.BSize.ToString() + " Блоков по 4 кб";
                if (FormsVar.BSize > 1048576)
                {
                    var cnt = (FormsVar.BSize * 4) / 1024;
                    label2.Text = Convert.ToString(cnt) + " MB";
                }
                else
                    if (FormsVar.BSize > 256)
                    {
                        var cnt = (FormsVar.BSize * 4096) / 1024 / 1024;
                        label2.Text = Convert.ToString(cnt) + " MB";
                    }
                    else
                    {
                        var cnt = (FormsVar.BSize * 4096) / 1024;
                        label2.Text = Convert.ToString(cnt) + " KB";
                    }
                GC.Collect();
        
        }

    }
}
