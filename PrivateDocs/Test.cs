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
        public int Size { get; set; }
        public Test(int Size, string Path)
        {
            FileSystem = new Controller(Path);
            this.Size = Size;
            this.Path = Path;
        }

        public void WritePrimaryBlock(int ContainerSize)
        {
            FileSystem.CreateSBManual(ContainerSize, FormsVar.Password);
            FileSystemIO.WriteFile(Path, FileSystem.CreateServiceInfo(), Constants.BLOCK_SIZE, 0);
            
        }
        public bool ReadContainer(string Path,string Password)
        {

            //byte[] tmp = FileSystemIO.ReadFile(Path,88);
            byte[] Pass = FileSystemIO.ReadFile(Path,16,72,16);
            string s = System.Text.Encoding.Unicode.GetString(Pass);

            byte[] compare = System.Text.Encoding.Unicode.GetBytes(Password);
            if (Password == s)
            {
                byte[] tmp = FileSystemIO.ReadFile(Path, 88, 0, 88);
                FileSystem.ReadServiceInfo(tmp);
                return true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Неправильный пароль!");
                return false;
            }
        }
        public List<string> ReadFiles()
        {
           return FileSystem.ReadFiles();
        }
        public void AddFile(string Path)
        {
            FileSystem.AddFileUpd(Path);
        }

        public void ReadFileFromFS(string CompareName,string OutputDir)
        {
            //FileSystem.ReadFileFromFS(@"C:\\test\\", FileSystem.ReadFiles(CompareName));
            FileSystem.ReadFileFromFS(OutputDir, FileSystem.ReadFiles(CompareName));
        }
    }

    public class FormsVar
    {
        public static int CSize;
        public static byte[] Password { get; set; }
        
        FormsVar()
        {
            CSize = 0;
            Password = new byte[Constants.MAX_PASSWORD_LENGTH];
        }
    }
}
