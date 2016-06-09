using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{// обертки-оберточки
   public class Test
    {
        Controller FileSystem;
        public string Path { get; set; }
        public Int64 Size { get; set; }
        public Test(Int64 Size, string Path)
        {
            FileSystem = new Controller(Path);
            this.Size = Size;
            this.Path = Path;
        }

        public void WritePrimaryBlock(Int64 ContainerSize)
        {
            FileSystem.CreateSBManual(ContainerSize, FormsVar.Password);
            FileSystem.CreateServiceInfo();
            //FileSystemIO.WriteFile(Path, FileSystem.CreateServiceInfo(), Constants.BLOCK_SIZE, 0);
            //GC.Collect();            
        }
        public bool OpenContainer(string Path, string Password)
        {
            string fix = Password;
            //fix.Remove(fix.IndexOf("\0"), fix.Length - fix.IndexOf("\0"));
            byte[] Passw = System.Text.Encoding.Unicode.GetBytes(fix);
            byte[] Pass = FileSystemIO.ReadFile(Path, 16, 72, 16, Passw);
            string s = System.Text.Encoding.Unicode.GetString(Pass);
            s = s.Remove(s.IndexOf("\0"), s.Length - s.IndexOf("\0"));
            if (Password == s)
            {
                FormsVar.Password = Pass;
                byte[] tmp = FileSystemIO.ReadFile(Path, 88, 0, 88, FormsVar.Password);
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
                byte[] tmp = FileSystemIO.ReadFile(Path, 88, 0, 88, FormsVar.Password);
                FileSystem.ReadServiceInfo(tmp); 
        }
        public List<string> ReadFiles()
        {
           return FileSystem.ReadFilesMEM();
        }
        public void AddFile(string Path)
        {
            FileSystem.AddFileUpd(Path);
        }

        public void ReadFileFromFS(string CompareName,string OutputDir)
        {
            //FileSystem.ReadFileFromFS(OutputDir, FileSystem.ReadFiles(CompareName));
            FileSystem.ReadFileFromFSlist(OutputDir, FileSystem.ReadFilesMEM(CompareName));
        }

        public void RemoveFile(string CompareName)
        {
            FileSystem.RemoveFile(FileSystem.ReadFilesMEM(CompareName));
        }

    }

    public class FormsVar
    {
        public static Int64 CSize;
        public static byte[] Password { get; set; }
        public static int BSize;
        FormsVar()
        {
            BSize = 0;
            CSize = 0;
            Password = new byte[Constants.MAX_PASSWORD_LENGTH];
        }
    }
}
