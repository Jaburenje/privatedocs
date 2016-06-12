using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{// обертки-оберточки
   public class Test
    {
        Controller FileSystem;
        public Int64 Size { get; set; }
        public Test(Int64 Size, string Path)
        {
            FileSystem = new Controller(Path);
            this.Size = Size;
        }

        public void WritePrimaryBlock(Int64 ContainerSize, BackgroundWorker worker, DoWorkEventArgs e)
        {
            FileSystem.CreateSBManual(ContainerSize, FormsVar.Password);
            FileSystem.CreateServiceInfo(worker,e);
            //FileSystemIO.WriteFile(Path, FileSystem.CreateServiceInfo(), Constants.BLOCK_SIZE, 0);
            //GC.Collect();            
        }
        public bool OpenContainer(string Password, BackgroundWorker worker, DoWorkEventArgs e)
        {
            string fix = Password;
            //fix.Remove(fix.IndexOf("\0"), fix.Length - fix.IndexOf("\0"));
            byte[] Passw = System.Text.Encoding.Unicode.GetBytes(fix);
            byte[] Super = Encryption.ReadFile(FormsVar.Path, Constants.BLOCK_SIZE, 0, Constants.BLOCK_SIZE, Passw); //16, 72, 16, Passw);
            byte[] Pass=new byte[Passw.Length];
            Buffer.BlockCopy(Super, 72, Pass, 0, Passw.Length);
            string s = System.Text.Encoding.Unicode.GetString(Pass);
            //s = s.Remove(s.IndexOf("\0"), s.Length - s.IndexOf("\0"));
            if (Passw.SequenceEqual(Pass))
            {
                FormsVar.Password = Pass;
                byte[] tmp = Encryption.ReadFile(FormsVar.Path, Constants.BLOCK_SIZE, 0, Constants.BLOCK_SIZE, FormsVar.Password);
                FileSystem.ReadServiceInfo(tmp);
                return true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Неправильный пароль!");
                return false;
            }
        }
        public void ReadContainer(string Path)
        {
                byte[] tmp = Encryption.ReadFile(Path, Constants.BLOCK_SIZE, 0, Constants.BLOCK_SIZE, FormsVar.Password);
                FileSystem.ReadServiceInfo(tmp); 
        }
        public List<string> ReadFiles(BackgroundWorker worker, DoWorkEventArgs e)
        {
           return FileSystem.ReadFilesMEM(worker,e);
        }
        public void AddFile(string Path, BackgroundWorker worker, DoWorkEventArgs e)
        {
            FileSystem.AddFileUpd(Path,worker,e);
        }

        public void ReadFileFromFS(string CompareName, BackgroundWorker worker, DoWorkEventArgs e)
        {
            //FileSystem.ReadFileFromFS(OutputDir, FileSystem.ReadFiles(CompareName));

            FileSystem.ReadFileFromFSlist(FormsVar.OutPath, FileSystem.ReadFilesMEM(CompareName));
        }

        public void RemoveFile(string CompareName)
        {
            FileSystem.RemoveFile(FileSystem.ReadFilesMEM(CompareName));
        }
        public void OpenFile(string CompareName, BackgroundWorker worker, DoWorkEventArgs e)
        {
            FileSystem.OpenFile(FileSystem.ReadFilesMEM(CompareName),worker,e);
        }
    }

    public class FormsVar
    {
        public static string Path;
        public static string OutPath;
        public static Int64 CSize;
        public static byte[] Password { get; set; }
        public static int BSize;
        public static bool Bvar;
        FormsVar()
        {
            Bvar = false;
            BSize = 0;
            CSize = 0;
            Password = new byte[Constants.MAX_PASSWORD_LENGTH];
        }
    }
}
