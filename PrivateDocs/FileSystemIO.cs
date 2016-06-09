using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    class FileSystemIO
    {
        public static byte[] ReadFile(string path)
        {
           byte[] data =  File.ReadAllBytes(path);
           return data;
        }

        public static void WriteFile(string path, byte[] data, int buffer, int offset, byte[] passwd)
        {
            //try
            //{
                FileStream FStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: buffer);
                FStream.Seek(offset, SeekOrigin.Begin);
                //FStream.SetLength(data.LongLength);
                FStream.Write(data, 0, data.Length);//0
                FStream.Close();
           // }
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex.Message);
            //}
        }

        public static byte[] ReadFile(string path, int buffer, byte[] passwd)
        {
            if (File.Exists(path)==false)
            {
                Debug.WriteLine("Error! File not exsists. " + path);
            }
            else
            {
                //try
                //{
                    byte[] buf = new byte[buffer];
                    FileStream ReadFStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: buffer);
                    int number;
                    List<byte> list = new List<byte>();
                    while ((number=ReadFStream.Read(buf,0,buf.Length))!=0)
                    {
                        list.AddRange(buf);
                    }
                    byte[] result = list.ToArray<byte>();
                    ReadFStream.Close();
                    return result;
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //}
            }
            return null;
        }

        public static byte[] ReadFile(string path, int buffer, int offset, int length, byte[] passwd)
        {
            if (File.Exists(path) == false)
            {
                Debug.WriteLine("Error! File not exsists. " + path);
            }
            else
            {
                //try
                //{
                    byte[] buf = new byte[buffer];
                    FileStream ReadFStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: buffer);
                    List<byte> list = new List<byte>();
                    ReadFStream.Seek(offset, SeekOrigin.Begin);
                    while (list.Count<length)
                    {
                        ReadFStream.Read(buf, 0, buf.Length);
                        list.AddRange(buf);
                    }
                    byte[] result = list.ToArray<byte>();
                    ReadFStream.Close();
                    return result;
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //}
            }
            return null;
        }
    }
}
