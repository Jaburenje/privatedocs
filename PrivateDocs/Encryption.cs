using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;

namespace PrivateDocs
{
    public static class Encryption
    {
        private static string KeySalt = "SuperPuperDuperSALT";
        
        private static  SymmetricAlgorithm CreateKey(byte[] passwd)
        {
            DeriveBytes keys = new Rfc2898DeriveBytes(passwd, Encoding.Unicode.GetBytes(KeySalt), passwd.Length);
            SymmetricAlgorithm key = new RijndaelManaged();
            key.BlockSize = 128;
            key.KeySize = 128;//256
            key.Key = keys.GetBytes(key.KeySize >> 3);
            key.IV = keys.GetBytes(key.BlockSize >> 3);
            key.Mode = CipherMode.ECB;
            key.Padding = PaddingMode.Zeros;
            return key;
        }

        public static void WriteFile(string path, byte[] data, int buffer, int offset,byte[] passwd)
        {
            try
            {
                FileStream FStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: buffer);
                FStream.Seek(offset, SeekOrigin.Begin);
                SymmetricAlgorithm SecretKey = CreateKey(passwd);
                using (var aes = new AesCryptoServiceProvider())
                {
                    aes.BlockSize = SecretKey.BlockSize;
                    aes.KeySize = SecretKey.KeySize;
                    aes.Key = SecretKey.Key;
                    aes.IV = SecretKey.IV;
                    aes.Mode = SecretKey.Mode;
                    aes.Padding = SecretKey.Padding;

                    CryptoStream CStream = new CryptoStream(FStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                    CStream.Write(data, 0, data.Length);
                    CStream.Close();
                }
                FStream.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static byte[] ReadFile(string path, int buffer,int offset, int length, byte[] passwd)
        {
            if (File.Exists(path) == false)
            {
                Debug.WriteLine("Error! File not exsists. " + path);
            }
            else
            {
                try
                {
                    byte[] buf = new byte[buffer];
                    FileStream ReadFStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: buffer);
                    List<byte> list = new List<byte>();
                    ReadFStream.Seek(offset, SeekOrigin.Begin);
                    SymmetricAlgorithm SecretKey = CreateKey(passwd);
                    using (var aes = new AesCryptoServiceProvider())
                    {
                        aes.BlockSize = SecretKey.BlockSize;
                        aes.KeySize = SecretKey.KeySize;
                        aes.Key = SecretKey.Key;
                        aes.IV = SecretKey.IV;
                        aes.Mode = SecretKey.Mode;
                        aes.Padding = SecretKey.Padding;
                        CryptoStream ReadCStream = new CryptoStream(ReadFStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                        while (list.Count < length)
                        {
                            ReadCStream.Read(buf, 0, buf.Length);
                            list.AddRange(buf);
                        }
                        ReadCStream.Close();
                    }
                    ReadFStream.Close();
                    return list.ToArray();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            return null;
        }
    
    }
}
