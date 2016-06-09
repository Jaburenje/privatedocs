using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    class inode_struct
    {
        public int Number { get; set;} //4 //номер inode
        public ushort FileType { get; set; } //2 тип файла(папка(10) или файл(20) 
        public int FileSize { get; set; } //4 размер в байтах

        public int[] BlockPointer { get; set; } //4*15=60 умножение на 15 т.е. 15 полей указатели на блоки с данными
        //70 
        private byte[] filling;    //дозаполнение до полноценного субблока
        public inode_struct()
        {
            Number = -1;
            BlockPointer = new int[Constants.INODE_BLOCKS];
            filling = new byte[Constants.INODE_SIZE - 70];
        }
        public inode_struct(int inode_index)
        {
            Number = inode_index;
            BlockPointer = new int[Constants.INODE_BLOCKS];
            filling = new byte[Constants.INODE_SIZE - 70];
        }
        
        public inode_struct(int Number,ushort FileType,int FileSize, int[] BlockPointer)
        {
            this.Number=Number;
            this.FileType = FileType;
            this.FileSize = FileSize;
            this.BlockPointer = new int[Constants.INODE_BLOCKS];
            for (var i=0;i<BlockPointer.Length;i++)
            {
                this.BlockPointer[i] = BlockPointer[i];
            }
            filling = new byte[Constants.INODE_SIZE - 70];
        }

        public inode_struct(byte[] inode_byte)
        {
            var offset = 0;

            byte[] Number = new byte[Marshal.SizeOf(this.Number)];
            Buffer.BlockCopy(inode_byte, offset, Number, 0, Number.Length);
            this.Number = BitConverter.ToInt32(Number, 0);
            offset += Number.Length;

            byte[] FileType = new byte[Marshal.SizeOf(this.FileType)];
            Buffer.BlockCopy(inode_byte, offset, FileType, 0, FileType.Length);
            this.FileType = BitConverter.ToUInt16(FileType, 0);
            offset += FileType.Length;
            
            byte[] FileSize = new byte[Marshal.SizeOf(this.FileSize)];
            Buffer.BlockCopy(inode_byte, offset, FileSize, 0, FileSize.Length);
            this.FileSize = BitConverter.ToInt32(FileSize, 0);
            offset += FileSize.Length;

            BlockPointer = new Int32[Constants.INODE_BLOCKS];
            Buffer.BlockCopy(inode_byte, offset, BlockPointer, 0, BlockPointer.Length * Marshal.SizeOf(BlockPointer[0]));
            offset += BlockPointer.Length * Marshal.SizeOf(BlockPointer[0]);

            filling = new byte[Constants.INODE_SIZE - 70];
        }
        
        public int GetSize()
        {
            return (Marshal.SizeOf(Number) + Marshal.SizeOf(FileType) + Marshal.SizeOf(FileSize) + (Marshal.SizeOf(BlockPointer[0])*Constants.INODE_BLOCKS) + filling.Length);
        }

        public byte[] Get()
        {
            var size = GetSize();
            byte[] result=new byte[size];
            int offset = 0;
            
            byte[] Number = BitConverter.GetBytes(this.Number);
            Buffer.BlockCopy(Number, 0, result, offset, Number.Length);
            offset += Number.Length;

            byte[] FileType = BitConverter.GetBytes(this.FileType);
            Buffer.BlockCopy(FileType, 0, result, offset, FileType.Length);
            offset += FileType.Length;

            byte[] FileSize = BitConverter.GetBytes(this.FileSize);
            Buffer.BlockCopy(FileSize, 0, result, offset, FileSize.Length);
            offset += FileSize.Length;

            Buffer.BlockCopy(BlockPointer, 0, result, offset, Marshal.SizeOf(BlockPointer[0]) * BlockPointer.Length);
            offset += Marshal.SizeOf(BlockPointer[0]) * BlockPointer.Length;

            Buffer.BlockCopy(filling, 0, result, offset, filling.Length);
            offset+=filling.Length;

            return result;
        }

        public void Set(int Number,ushort FileType, int FileSize,int[] BlockPointer)
        {
            this.Number = Number;
            this.FileType = FileType;
            this.FileSize = FileSize;
            this.BlockPointer = BlockPointer;
            filling = new byte[Constants.INODE_SIZE - 70];

        }

        public void Set(byte[] value)
        {
            var offset = 0;

            byte[] Number = new byte[Marshal.SizeOf(this.Number)];
            Buffer.BlockCopy(value, offset, Number, 0, Number.Length);
            this.Number = BitConverter.ToInt32(Number, 0);
            offset += Number.Length;

            byte[] FileType = new byte[Marshal.SizeOf(this.FileType)];
            Buffer.BlockCopy(value, offset, FileType, 0, FileType.Length);
            this.FileType = BitConverter.ToUInt16(FileType, 0);
            offset += FileType.Length;

            byte[] FileSize = new byte[Marshal.SizeOf(this.FileSize)];
            Buffer.BlockCopy(value, offset, FileSize, 0, FileSize.Length);
            this.FileSize = BitConverter.ToInt32(FileSize, 0);
            offset += FileSize.Length;

            BlockPointer = new Int32[Constants.INODE_BLOCKS];
            Buffer.BlockCopy(value, offset, BlockPointer, 0, BlockPointer.Length * Marshal.SizeOf(BlockPointer[0]));
            offset += BlockPointer.Length * Marshal.SizeOf(BlockPointer[0]);

            filling = new byte[Constants.INODE_SIZE - 70];
        }
    }


}
