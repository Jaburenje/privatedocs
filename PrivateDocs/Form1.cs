using System;
using System.Collections;
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
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = false;
            InitializeBackgroundWorker();
            //Test test = new Test(1,"C:\\");
            Closed();
        }
        private void Opened()
        {
            button1.Enabled = false;
            button7.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button2.Enabled = true;
        }
        private void Closed()
        {
            button1.Enabled = true;
            button7.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button2.Enabled = true;
        }
        private void AllLocked()
        {
            button1.Enabled = false;
            button7.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button2.Enabled = false;
        }
        private void InitializeBackgroundWorker()
        {
            //backgroundWorker1.DoWork +=
               //new DoWorkEventHandler(backgroundWorker1_DoWork);
            //backgroundWorker1.RunWorkerCompleted +=new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged +=new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
        }


        #region background1 Создание контейнера
        private void backgroundWorker1_RunWorkerCompleted(
            object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                this.progressBar1.Value = 0;
            }

        }
        //===========================================================================================================

        private void backgroundWorker1_DoWork1(object sender,
            DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            test.WritePrimaryBlock(test.Size, worker, e); 
        }
#endregion

        //===========================================================================================================
        private void backgroundWorker1_ProgressChanged(object sender,
            ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = e.ProgressPercentage;
        }

        //===========================================================================================================
        private void button1_Click(object sender, EventArgs e)
        {
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
                    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork1);
                    backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
                    backgroundWorker1.RunWorkerAsync(test.Size);
                    while (this.backgroundWorker1.IsBusy)
                    {
                        progressBar1.Increment(1);
                        Application.DoEvents();
                    }
                    backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
                    backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork1);

                }
                backgroundWorker1.Dispose();
            }
            GC.Collect();            
            
        }
        #region background2 Открытие контейнера
        private void backgroundWorker1_DoWork2(object sender,
 DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = test.OpenContainer(System.Text.Encoding.Unicode.GetString(FormsVar.Password), worker, e);
            //test.WritePrimaryBlock(test.Size, worker, e);
        }

        private void backgroundWorker1_RunWorkerCompleted2(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                FormsVar.Bvar = false;
                MessageBox.Show(e.Error.Message);
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                FormsVar.Bvar = (bool)e.Result;
            }

        }
    
        #endregion

        #region background3 Чтение списка файлов
        private void backgroundWorker1_DoWork3(object sender,
 DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = test.ReadFiles(worker, e);
            //System.Windows.Forms.MessageBox.Show("KEK2EKE");
            //test.WritePrimaryBlock(test.Size, worker, e);
        }
        //===========================================================================================================
        private void backgroundWorker1_RunWorkerCompleted3(
            object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                //List<string> list = new List<string>();
                checkedListBox1.Items.Clear();
                if (e.Result is IEnumerable)
                {
                    List<object> list = new List<object>();
                    var enumerator = ((IEnumerable) e.Result).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                    list.Add(enumerator.Current);
                    }
                    for (var i = 0; i < list.Count; i++)
                        checkedListBox1.Items.Add(list[i].ToString());
                }
                
                this.progressBar1.Value = 0;
            }

        }
        #endregion

        #region background4 Добавление файла

        private void backgroundWorker1_DoWork4(object sender,
            DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if ((string)e.Argument!=null)
            test.AddFile((string)e.Argument, worker, e);
        }

        #endregion
        #region background5 Выгрузка файла
        private void backgroundWorker1_DoWork5(object sender,
            DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            test.ReadFileFromFS((string)e.Argument, worker, e);
        }
        #endregion
        private void button2_Click(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Form3 passForm = new Form3();
                passForm.Owner = this;
                passForm.ShowDialog();
                if (passForm.DialogResult==DialogResult.OK)
                {
                    test = new Test(1, openFileDialog1.FileName);
                    if (FormsVar.Password != null)
                    {
                        
                        FormsVar.Path=openFileDialog1.FileName;
                        backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted2);
                        backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork2);
                        backgroundWorker1.RunWorkerAsync();
                        while (this.backgroundWorker1.IsBusy)
                        {
                            progressBar1.Increment(1);
                            Application.DoEvents();
                        }
                        backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted2);
                        backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork2);
                        if (FormsVar.Bvar)
                        {
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
                            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
                            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork3);
                            backgroundWorker1.RunWorkerAsync();
                            while (this.backgroundWorker1.IsBusy)
                            {
                                //progressBar1.Increment(1);
                                Application.DoEvents();
                            }
                            backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
                            backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork3);
                            Opened();
                        }
                    }
                }
            }
            backgroundWorker1.Dispose();
            GC.Collect();            
        }


        private void backgroundWorker1_RunWorkerCompleted5(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                this.progressBar1.Value = 0;
            }

        }
        
        private void button3_Click(object sender, EventArgs e)
        {
            AllLocked();
            //Test test = new Test(1, textBox2.Text);
            if (FormsVar.Password!=null)
            {
                test.ReadContainer(FormsVar.Path);
                openFileDialog1.InitialDirectory = "C:\\";
                openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //System.Windows.Forms.MessageBox.Show("KEKEKE");
                    backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted5);
                    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork4);
                    backgroundWorker1.RunWorkerAsync(openFileDialog1.FileName);
                    while (this.backgroundWorker1.IsBusy)
                    {
                        progressBar1.Increment(1);
                        Application.DoEvents();
                    }
                    backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork4);
                    backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted5);

                    //test.AddFile(openFileDialog1.FileName,wrk,be);
                    //test.ReadContainer(FormsVar.Path);
                    checkedListBox1.Items.Clear();

                    backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
                    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork3);
                    backgroundWorker1.RunWorkerAsync();
                    while (this.backgroundWorker1.IsBusy)
                    {
                        progressBar1.Increment(1);
                        Application.DoEvents();
                    }
                    backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork3);
                    backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);

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
            Opened();
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

        private void button4_Click(object sender, EventArgs ee)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
                if( result == DialogResult.OK )
            {
                AllLocked();
                //Test test = new Test(1, textBox2.Text);
                test.ReadContainer(FormsVar.Path);

                foreach (int indexChecked in checkedListBox1.CheckedIndices)
                {
                    FormsVar.OutPath = folderBrowserDialog1.SelectedPath;
                    string tmp = checkedListBox1.Items[indexChecked].ToString();
                    backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
                    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork5);
                    backgroundWorker1.RunWorkerAsync(tmp);
                    while (this.backgroundWorker1.IsBusy)
                    {
                        progressBar1.Increment(1);
                        Application.DoEvents();
                    }
                    backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork5);
                    backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
                    //test.ReadFileFromFS(tmp,folderBrowserDialog1.SelectedPath,wrk,be);
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
                Opened();
                GC.Collect();            


           //    Test test = new Test(1, textBox2.Text);
           //    test.ReadContainer(test.Path);

           //foreach (int indexChecked in checkedListBox1.CheckedIndices)
           //{
           //    string tmp = checkedListBox1.Items[indexChecked].ToString();
           //    test.ReadFileFromFS(tmp);
           //}
        }
        #region Открытие файла
        private void backgroundWorker1_RunWorkerCompleted6(
            object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else
            {
                this.progressBar1.Value = 0;
            }

        }
        //===========================================================================================================

        private void backgroundWorker1_DoWork6(object sender,
            DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (e.Argument!=null)
            test.OpenFile((string)e.Argument,worker,e);
        }
        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            AllLocked();
                //if (test.ReadContainer(test.Path))
                    test.ReadContainer(FormsVar.Path);

                    foreach (int indexChecked in checkedListBox1.CheckedIndices)
                    {
                        string tmp = checkedListBox1.Items[indexChecked].ToString();
                        test.RemoveFile(tmp);
                        
                        //        checkedListBox1.Items.Clear();
                        //        List<string> list = new List<string>();
                        //        BackgroundWorker wrk = sender as BackgroundWorker;
                        //        //list = test.ReadFiles(wrk,be);
                        //        for (var i = 0; i < list.Count; i++)
                        //            checkedListBox1.Items.Add(list[i]);
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
                backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
                backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork3);
                backgroundWorker1.RunWorkerAsync();
                while (this.backgroundWorker1.IsBusy)
                {
                    progressBar1.Increment(1);
                    Application.DoEvents();
                }
                backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork3);
                backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
							
                GC.Collect();
                Opened();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            AllLocked();
            test.ReadContainer(FormsVar.Path);
            foreach (int indexChecked in checkedListBox1.CheckedIndices)
            {
                string tmp = checkedListBox1.Items[indexChecked].ToString();
                backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted6);
                backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork6);
                backgroundWorker1.RunWorkerAsync(tmp);
                while (this.backgroundWorker1.IsBusy)
                {
                    progressBar1.Increment(1);
                    Application.DoEvents();
                }
                backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork6);

                backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted6);
                //test.OpenFile(tmp);
                //if (test.ReadContainer(test.Path))//, System.Text.Encoding.Unicode.GetString(FormsVar.Password)))
                //{
                checkedListBox1.Items.Clear();
                backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
                backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork3);
                backgroundWorker1.RunWorkerAsync();
                while (this.backgroundWorker1.IsBusy)
                {
                    progressBar1.Increment(1);
                    Application.DoEvents();
                }
                backgroundWorker1.DoWork -= new DoWorkEventHandler(backgroundWorker1_DoWork3);
                backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted3);
                break;
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
            Opened();
            GC.Collect();
        }

    }
}
