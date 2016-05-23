using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    class cat
    {
        public int Inode_num; //4 //номер i-узла файла
        public int Size; //4
        public ushort Type; //2
        public byte[] Name;  //118
        //128
        public cat()
        {
            Name = new byte[Constants.MAX_NAME_LENGTH];
            Size = 0;
            Type = 0;
            Inode_num = 0;
        }

        public cat(int Inode_num,int Size,byte[] Name,ushort Type)
        {
            this.Inode_num = Inode_num;
            this.Type = Type;
            this.Size = Size;
            this.Name = Name;
        }

        public int GetSize()
        {
            return Marshal.SizeOf(Inode_num) + Marshal.SizeOf(Size)+ Marshal.SizeOf(Type) + Name.Length;
        }

        public byte[] Get()
        {
            var size = GetSize();
            byte[] result = new byte[size];
            int offset = 0;

            byte[] inode_num = BitConverter.GetBytes(this.Inode_num);
            Buffer.BlockCopy(inode_num, 0, result, offset, inode_num.Length);
            offset += inode_num.Length;

            byte[] Size = BitConverter.GetBytes(this.Size);
            Buffer.BlockCopy(Size, 0, result, offset, Size.Length);
            offset += Size.Length;

            byte[] Type = BitConverter.GetBytes(this.Type);
            Buffer.BlockCopy(Type, 0, result, offset, Type.Length);
            offset += Type.Length;

            Buffer.BlockCopy(this.Name, 0, result, offset, this.Name.Length);
            offset += Name.Length;

            return result;
        }

        public void Set(byte[] value)
        {
            var offset = 0;

            byte[] inode = new byte[Marshal.SizeOf(this.Inode_num)];
            Buffer.BlockCopy(value, offset, inode, 0, inode.Length);
            this.Inode_num = BitConverter.ToInt32(inode, 0);
            offset += inode.Length;

            byte[] size = new byte[Marshal.SizeOf(this.Size)];
            Buffer.BlockCopy(value, offset, size, 0, size.Length);
            this.Size = BitConverter.ToInt32(size, 0);
            offset += size.Length;

            byte[] type = new byte[Marshal.SizeOf(this.Type)];
            Buffer.BlockCopy(value, offset, type, 0, type.Length);
            this.Type = BitConverter.ToUInt16(type, 0);
            offset += type.Length;

            Name = new byte[Constants.MAX_NAME_LENGTH];
            Buffer.BlockCopy(value, offset, this.Name, 0, Name.Length);
            offset += Name.Length; 
        }


    }
}
