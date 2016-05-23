﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
   public class Test
    {
        Controller FileSystem;
        public string Path { get; set; }
        public byte[] Password { get; set; }

        public int Size { get; set; }

        public Test(int Size, string Path)
        {
            FileSystem = new Controller(Path);
            this.Size = Size;
            this.Path = Path;
            Password = new byte[Constants.MAX_PASSWORD_LENGTH];
        }

        public void WritePrimaryBlock(int ContainerSize)
        {
            FileSystem.CreateSBManual(ContainerSize);
            FileSystemIO.WriteFile(Path, FileSystem.CreateServiceInfo(), Constants.BLOCK_SIZE, 0);
            
        }
        public void ReadContainer(string Path)
        {

            byte[] tmp = FileSystemIO.ReadFile(Path,88);
            FileSystem.ReadServiceInfo(tmp);
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
}
