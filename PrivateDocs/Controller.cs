using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    class Controller
    {
       public SB SuperBlock;
       public Bitmap_Blocks InodeBitmap;
       public Bitmap_Blocks Blocks_Bitmap;
       public inode_struct Inode;//текущая директория
       //public inode_struct[] Inode_table;
       public string OpenPath;
       public string Password;
       List<int> blockstest;
    public Controller(string Path)
    {
    SuperBlock=new SB();
    InodeBitmap = null;
    Blocks_Bitmap = null;
    Inode = null;
    //Inode_table = null;
    OpenPath = Path;
    }
        /// <summary>
        /// Создание контейнера
        /// </summary>
        /// <param name="ContainerSize">РАЗМЕР В БАЙТАХ</param>
        public void CreateSBManual(Int64 ContainerSize, byte[] pass)
        {
            Int64 blockssize = ContainerSize / Constants.BLOCK_SIZE;
            SuperBlock.sb_blocks_count = (Int32)blockssize;
            SuperBlock.sb_block_size = Constants.BLOCK_SIZE;
            SuperBlock.sb_check_time = Constants.CHECK_INTERVAL;
            SuperBlock.sb_free_blocks_count = SuperBlock.sb_blocks_count - 1;
            SuperBlock.Password = pass;
            FormsVar.BSize = SuperBlock.sb_free_blocks_count;
        }
        public void ReadServiceInfo(byte[] SBlock)
        {
            SB ReadedSB = new SB(SBlock);
            SuperBlock = ReadedSB;
            byte[] BBlock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, (SuperBlock.sb_block_map_index - 1) * Constants.BLOCK_SIZE, FormsVar.Password);
            Bitmap_Blocks BB = new Bitmap_Blocks(BBlock);
            Blocks_Bitmap = BB;
            byte[] IBlock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE * SuperBlock.sb_block_map_index, (SuperBlock.sb_inodes_map_index - SuperBlock.sb_block_map_index) * Constants.BLOCK_SIZE, FormsVar.Password);
            Bitmap_Blocks IB = new Bitmap_Blocks(IBlock);
            InodeBitmap = IB;
            byte[] ind = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE * (SuperBlock.sb_inodes_map_index), Constants.BLOCK_SIZE, FormsVar.Password);
            Inode = new inode_struct(0);
            Inode.Set(ind);

            //var offset=0;
            //byte[] ITable = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE * SuperBlock.sb_inodes_map_index, (SuperBlock.sb_first_data_block-SuperBlock.sb_inodes_map_index-SuperBlock.sb_block_map_index-1) * Constants.BLOCK_SIZE);
            //this.Inode_table = new inode_struct[InodeBitmap.TotalBlocks];
            //for (var i = 0; i < InodeBitmap.TotalBlocks;i++ )
            //{
            //    byte[] tmp = new byte[Constants.INODE_SIZE];
            //    Buffer.BlockCopy(ITable, offset, tmp, 0, tmp.Length);
            //    offset += tmp.Length;
            //    this.Inode_table[i] = new inode_struct();

            //    this.Inode_table[i].Set(tmp);
            //}
            FormsVar.BSize = SuperBlock.sb_free_blocks_count;
        }

        public void CreateServiceInfo(BackgroundWorker worker, DoWorkEventArgs e)//byte[]
        {
            worker.ReportProgress(0);
            Blocks_Bitmap = new Bitmap_Blocks(SuperBlock.sb_blocks_count);
            var bytesize = Blocks_Bitmap.GetSize();
            var bb_size = bytesize;
            var blocksize = (bytesize / Constants.BLOCK_SIZE + (bytesize % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            var bbl_size = blocksize;
            //посчитали блок битмапов
            Bitmap_Blocks preInodeBitmap = new Bitmap_Blocks(SuperBlock.sb_free_blocks_count-blocksize); //удален суперблок и битовая карта
            bytesize+=preInodeBitmap.GetSize(); 
            var ib_size = preInodeBitmap.GetSize();
            var ib_bl_size = (ib_size / Constants.BLOCK_SIZE + (ib_size % Constants.BLOCK_SIZE > 0 ? 1 : 0));

            int inodes_count = (SuperBlock.sb_free_blocks_count - bbl_size - ib_bl_size);//подсчет без суперблока и битовых карт
            if ((inodes_count*128)%4096!=0)
            {
                int temp = ((inodes_count * 128) % 4096)/128;
                inodes_count -= temp;
            }
            //чтобы ровно все подсчитать, сделаем предрасчет сколько займут структуры 1 структура 128байт
            int inode_struct_size = ((inodes_count * Constants.INODE_SIZE) / Constants.BLOCK_SIZE) + ((inodes_count * Constants.INODE_SIZE) % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            inodes_count -= inode_struct_size;
            if ((inodes_count * 128) % 4096 != 0)
            {
                int temp = ((inodes_count * 128) % 4096) / 128;
                inodes_count -= temp;
            }
            InodeBitmap = new Bitmap_Blocks(inodes_count);
            ib_size = InodeBitmap.GetSize();
            ib_bl_size = (ib_size / Constants.BLOCK_SIZE + (ib_size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            worker.ReportProgress(20);
            //перерасчитанная структура с учетом метс акоторое уже будет занято inode_table и bitmapами
            inode_struct[] Inodes = new inode_struct[inodes_count];

            Parallel.ForEach(Partitioner.Create(0, Inodes.Length),
                (range, state) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        Inodes[i] = new inode_struct(i);
                    }
                });

            #region BitmapFilling
            var filledBlocks = (bbl_size + ib_bl_size + inode_struct_size+1)/Constants.BIT_IN_BYTE_COUNT;//тут карочи берем количество блоков и делим их на 8(биты в байтах) чтобы занять байтовую переменную правильно при заполнении битмапа
            int mod = (bbl_size + ib_bl_size + inode_struct_size + 1) % Constants.BIT_IN_BYTE_COUNT;

            for (var i=0;i<filledBlocks;++i)
            {
                Blocks_Bitmap.indexMap[i] = 255;
                Blocks_Bitmap.FreeBlocks -= Constants.BIT_IN_BYTE_COUNT;
            }
            if (mod>0)
            {
                var tmp=(bbl_size + ib_bl_size + inode_struct_size + 1) / Constants.BIT_IN_BYTE_COUNT;
                var tmp2 = Math.Pow(2, mod) - 1;
                Blocks_Bitmap.indexMap[tmp] = Convert.ToByte(tmp2);
                Blocks_Bitmap.FreeBlocks -= mod;
            }
            #endregion
            


            #region FillingSuperBlock
            SuperBlock.sb_free_blocks_count = Blocks_Bitmap.FreeBlocks;
            SuperBlock.sb_block_map_index = bbl_size+1;
            SuperBlock.sb_inodes_map_index = SuperBlock.sb_block_map_index + ib_bl_size;
            SuperBlock.sb_free_inodes_count = InodeBitmap.FreeBlocks;
            SuperBlock.sb_mount_time = -1;
            SuperBlock.sb_write_time = -1;
            SuperBlock.sb_mount_count = 0;
            SuperBlock.sb_inodes_count = inodes_count;
            SuperBlock.sb_block_size = Constants.BLOCK_SIZE;
            SuperBlock.sb_first_data_block = Blocks_Bitmap.TotalBlocks - Blocks_Bitmap.FreeBlocks+1;
            SuperBlock.sb_errors = 0;
            SuperBlock.sb_state = 'c';
            SuperBlock.sb_last_check = -1;
            SuperBlock.sb_check_time = Constants.CHECK_INTERVAL;
            SuperBlock.sb_mount_max = Constants.MAX_MOUNT_COUNT;       
            #endregion
            //Inode = CreateRootDirectory(Inodes[0]);
            //Inodes[0] = Inode;
            //cat[] catRecords = new cat[Constants.BLOCK_SIZE / Constants.INODE_SIZE];
            //for (int i=0;i<catRecords.Length;i++)
            //{
            //    catRecords[i] = new cat();
            //}
            //byte[] byteroot = new byte[Constants.BLOCK_SIZE];
            //var offset = 0;
            //foreach (cat c in catRecords)
            //{
            //    byte[] block = c.Get();
            //    Buffer.BlockCopy(block, 0, byteroot, offset, block.Length);
            //    offset += block.Length;
            //}
            int filelength = GetSB().Length + GetBitmap().Length + GetIBitmap().Length;
            Encryption.WriteFile(OpenPath, GetSB(), Constants.BLOCK_SIZE, 0, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetBitmap(), Constants.BLOCK_SIZE, GetSB().Length, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetIBitmap(), Constants.BLOCK_SIZE, GetSB().Length + GetBitmap().Length, FormsVar.Password);
            worker.ReportProgress(30);
            int inoffset = GetINodes2(Inodes, filelength);
            worker.ReportProgress(50);
            //AddCatalogStructure(inoffset);
            worker.ReportProgress(70);
            AddCatalogStructureMEM(inoffset);
            worker.ReportProgress(100);
            //Encryption.WriteFile(OpenPath, GetINodes(Inodes), Constants.BLOCK_SIZE, 0);
            //Encryption.WriteFile(OpenPath, byteroot, Constants.BLOCK_SIZE, inoffset);
            
            //return result;
        }
        /// <summary>
        /// Сборщик сервисных блоков фс в байтовый массив
        /// </summary>
        /// <returns></returns>
        //public static byte[] Assembly(params byte[][] blocks)
        //{
        //    var size = 0;
        //    foreach (byte[] block in blocks)
        //    {
        //        size += block.Length;
        //    }

        //    byte[] result = new byte[size];
        //    var offset = 0;

        //    foreach (byte[] block in blocks)
        //    {
        //        Buffer.BlockCopy(block, 0, result, offset, block.Length);
        //        offset += block.Length;
        //    }

        //    return result;
        //}


        /// <summary>
        /// Возврат блока с суперблоком
        /// </summary>
        /// <returns></returns>
        public byte[] GetSB()
        {
            byte[] result = new byte[Constants.BLOCK_SIZE];
            var offset = 0;
            byte[] super = SuperBlock.Get();
            Buffer.BlockCopy(super, 0, result, offset, super.Length);
            return result;
        }
        /// <summary>
        /// Возврат битовой карты блоков 
        /// </summary>
        /// <returns></returns>
        public byte[] GetBitmap()
        {
            byte[] BBitmap = Blocks_Bitmap.Get();
            var Bitmap_Size = BBitmap.Length / Constants.BLOCK_SIZE + (BBitmap.Length % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            byte[] result = new byte[Bitmap_Size * Constants.BLOCK_SIZE];
            Buffer.BlockCopy(BBitmap, 0, result, 0, BBitmap.Length);
            return result;
        }
        /// <summary>
        /// Возврат битовой карты i-узлов
        /// </summary>
        /// <returns></returns>
        public byte[] GetIBitmap()
        {
            byte[] IBitmap = InodeBitmap.Get();
            var Bitmap_Size = IBitmap.Length / Constants.BLOCK_SIZE + (IBitmap.Length % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            byte[] result = new byte[Bitmap_Size * Constants.BLOCK_SIZE];
            Buffer.BlockCopy(IBitmap, 0, result, 0, IBitmap.Length);
            return result;
        }
        public inode_struct GetInode(int inodenum)
        {
            inode_struct Inode = new inode_struct(inodenum);
            var size = Inode.GetSize();
            byte[] inode = Encryption.ReadFile(OpenPath, size, size * inodenum + (SuperBlock.sb_inodes_map_index) * Constants.BLOCK_SIZE, size, FormsVar.Password);
            Inode.Set(inode);
            return Inode;
        }

        public void RewriteInode(int inodenum,inode_struct Inode)
        {
            byte[] data = Inode.Get();
            int size = data.Length;
            Encryption.WriteFile(OpenPath, data, size, size * inodenum + (SuperBlock.sb_inodes_map_index) * Constants.BLOCK_SIZE, FormsVar.Password);
        }

        public byte[] GetINodes(inode_struct[] inodes)
        {
            int test2 = inodes[0].GetSize();
            int test3 = inodes.Length;
            Int64 mas = test2 * test3;
            byte[] In_arr = new byte[mas/2];//[inodes.Length * inodes[0].GetSize()];
            byte[] inode = null;
            In_arr = new byte[mas];
            var offset = 0;
            foreach(inode_struct ind in inodes)
            {
                inode = ind.Get();

                Buffer.BlockCopy(inode, 0, In_arr, offset, inode.Length);
                offset += inode.Length;
            }
            var block_count = (In_arr.Length / Constants.BLOCK_SIZE) + (In_arr.Length % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            byte[] result = new byte[block_count * Constants.BLOCK_SIZE];//ошибка тут
            Buffer.BlockCopy(In_arr, 0, result, 0, In_arr.Length);

            return result;
        }

        public int GetINodes2(inode_struct[] inodes,int fileoffset)
        {
            int test2 = inodes[0].GetSize();
            int test3 = inodes.Length;
            Int64 remaining = test2 * test3;
            int gloffset = fileoffset;
            int lastpart = 0;
            byte[] In_arr;
            if (remaining>134217728)
            {
               In_arr = new byte[134217728];//[inodes.Length * inodes[0].GetSize()];//128mb
               byte[] inode = null;
               var offset = 0;
               foreach (inode_struct ind in inodes)
               {
                   inode = ind.Get();
                   Buffer.BlockCopy(inode, 0, In_arr, offset, inode.Length);
                   offset += inode.Length;
                  //gloffset += inode.Length;
                   remaining -= inode.Length;
                   if ((offset == 134217728)||(remaining==0))
                   {
                       Encryption.WriteFile(OpenPath, In_arr, In_arr.Length, gloffset, FormsVar.Password);
                       if ((offset != 134217728) && (remaining == 0))
                           gloffset += offset;
                       else
                       gloffset += 134217728;
                       offset = 0;
                       if ((remaining < 134217728)&&(remaining!=0))
                       {
                           In_arr = new byte[remaining];
                           offset = 0;
                       }
                   }
               }
               return gloffset;
            }
            else
            {
                In_arr = new byte[inodes.Length * inodes[0].GetSize()];////128mb
                byte[] inode = null;
                var offset = 0;
                foreach (inode_struct ind in inodes)
                {
                    inode = ind.Get();
                    Buffer.BlockCopy(inode, 0, In_arr, offset, inode.Length);
                    offset += inode.Length;
                }
                Encryption.WriteFile(OpenPath, In_arr, In_arr.Length, fileoffset, FormsVar.Password);
                return In_arr.Length + fileoffset;

            }
        }
       
        
        public inode_struct CreateRootDirectory(inode_struct root)
        {
            root.Set(0,10, 0, new int[Constants.INODE_BLOCKS]);
            int freeblock = (int)Blocks_Bitmap.GetFreeBlock();// заняли блок
            int iblock = (int)InodeBitmap.GetFreeBlock();
            root.Number = (int)iblock;
            Debug.Assert(!freeblock.Equals(-1));
            root.BlockPointer[0] = freeblock;
            Blocks_Bitmap.SetBitmapState((int)root.BlockPointer[0], true);
            SuperBlock.sb_free_blocks_count -= 1;
            InodeBitmap.SetBitmapState((int)root.Number,true);
            SuperBlock.sb_free_inodes_count -= 1;
            return root;
        }

        public void AddCatalogStructure(int offset)
        {
            int capacity = 32768;
            List<cat> catalogs = new List<cat>();
            int size=capacity*Constants.CAT_SIZE;
            int SizeinBlocks = (size / Constants.BLOCK_SIZE + (size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            List<int> blocks = new List<int>();
            blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks);
            Blocks_Bitmap.SetBitmapState(blocks, true);
            for (var i=0;i<blocks.Count;i++)
            {
                for (var j=0;j<32;j++)
                {
                    cat temp = new cat();
                    if ((i == 0)&&(j==0))
                    {
                        temp.Size = -1;
                        temp.Inode_num = 0;
                        temp.Type = 10;
                    }
                    byte[] data = temp.Get();
                    Encryption.WriteFile(OpenPath, data, data.Length, offset, FormsVar.Password);
                    offset += data.Length;
                }  
            }
            int filelength = GetSB().Length + GetBitmap().Length + GetIBitmap().Length;
            Encryption.WriteFile(OpenPath, GetSB(), Constants.BLOCK_SIZE, 0, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetBitmap(), Constants.BLOCK_SIZE, GetSB().Length, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetIBitmap(), Constants.BLOCK_SIZE, GetSB().Length + GetBitmap().Length, FormsVar.Password);
        }


        public void AddCatalogStructureMEM(int offset)
        {
            int capacity = 32768;
            int foffset = offset;
            int offset1 = 0;
            byte[] result = new byte[4194304];
            List<cat> catalogs = new List<cat>();
            int size = capacity * Constants.CAT_SIZE;
            int SizeinBlocks = (size / Constants.BLOCK_SIZE + (size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            List<int> blocks = new List<int>();
            blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks);
            Blocks_Bitmap.SetBitmapState(blocks, true);
            for (var i = 0; i < blocks.Count; i++)
            {
                for (var j = 0; j < 32; j++)
                {
                    
                    cat temp = new cat();
                    if ((i == 0) && (j == 0))
                    {
                        temp.Size = -1;
                        temp.Inode_num = 0;
                        temp.Type = 10;
                    }
                    byte[] data = temp.Get();
                    Buffer.BlockCopy(data,0,result,offset1,data.Length);
                    //Encryption.WriteFile(OpenPath, data, data.Length, offset); 
                    offset1 += data.Length;
                }
                
            }
            Encryption.WriteFile(OpenPath, result, result.Length, foffset, FormsVar.Password);
            int filelength = GetSB().Length + GetBitmap().Length + GetIBitmap().Length;
            Encryption.WriteFile(OpenPath, GetSB(), Constants.BLOCK_SIZE, 0, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetBitmap(), Constants.BLOCK_SIZE, GetSB().Length, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetIBitmap(), Constants.BLOCK_SIZE, GetSB().Length + GetBitmap().Length, FormsVar.Password);
        }

        public List<cat> ReadCatRecords(int block)
        {
            List<cat> result = new List<cat>();
            int offset=0;
            byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, block * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
            for (var i=0;i<data.Length/Constants.CAT_SIZE;i++)
            {
                byte[] part = new byte[Constants.CAT_SIZE];
                Buffer.BlockCopy(data, offset, part, 0, part.Length);
                offset += part.Length;
                cat tmp = new cat();
                tmp.Set(part);
                result.Add(tmp);
            }
            return result;
        }

        public List<cat> ReadCatRecords(List<int> block)
        {
            List<cat> result = new List<cat>();
            int offset = 0;
            for (var j=0;j<block.Count;j++)
            {
                byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, block[j] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                for (var i = 0; i < data.Length / Constants.CAT_SIZE; i++)
                {
                byte[] part = new byte[Constants.CAT_SIZE];
                Buffer.BlockCopy(data, offset, part, 0, part.Length);
                offset += part.Length;
                cat tmp = new cat();
                tmp.Set(part);
                result.Add(tmp);
                }
            }
            return result;
        }


        public void RewriteCatRecord(int inodenum, cat record)
        {
            int offset = 0;
            //
            //32 inodes per block
            //
            int count=(((InodeBitmap.TotalBlocks*128)+(SuperBlock.sb_inodes_map_index*Constants.BLOCK_SIZE))/Constants.BLOCK_SIZE)+SuperBlock.sb_inodes_map_index;
            int block = inodenum * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
            //
            byte[] data = record.Get();
            //byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE);
            Encryption.WriteFile(OpenPath, data, Constants.CAT_SIZE, block, FormsVar.Password);
            //for (var i = 0; i < data.Length / Constants.CAT_SIZE; i++)
            //{
            //    byte[] part = new byte[Constants.CAT_SIZE];
            //    Buffer.BlockCopy(data, offset, part, 0, part.Length);
            //    cat tmp = new cat();
            //    tmp.Set(part);
            //    string s1=System.Text.Encoding.Unicode.GetString(tmp.Name);
            //    string s2=System.Text.Encoding.Unicode.GetString(record.Name);
            //    if (s1==s2)
            //    {
            //        cat tmp2 = new cat();
            //        byte[] nullblock = tmp2.Get();
            //        Buffer.BlockCopy(nullblock, 0, data, offset, nullblock.Length);
            //        Encryption.WriteFile(OpenPath, data, Constants.BLOCK_SIZE, block * Constants.BLOCK_SIZE);
            //    }
            //    offset += part.Length;
            //}
        }

        public void RemoveFile(cat record)
        {
            inode_struct Inode = new inode_struct();
            Inode = GetInode(record.Inode_num);
            int sizeinblocks = (record.Size / Constants.BLOCK_SIZE + (record.Size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            int first = Inode.BlockPointer[0];
            List<int> blocks=new List<int>();
                for (var i = 0; i < 12; i++)
                    if (Inode.BlockPointer[i] != 0)
                        blocks.Add((int)Inode.BlockPointer[i]);

            if (sizeinblocks > 12)
            {
                //read 13 pointer and get list of addresses
                int offset2 = 0;
                byte[] adrblock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[12] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                List<int> list = new List<int>();
                for (var i = 0; i < 1024; i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset2, tmp, 0, sizeof(int));
                    offset2 += sizeof(int);
                    if (BitConverter.ToInt32(tmp, 0) != 0)
                        blocks.Add(BitConverter.ToInt32(tmp, 0));
                }
                Inode.BlockPointer[12] = 0;
            }
            if ((sizeinblocks>1036))
            {
                int offset3 = 0;
                //read 14 pointer for list of adresses
                byte[] adrblock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[13] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                List<int> list = new List<int>();
                for (var i = 0; i < 1024; i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset3, tmp, 0, sizeof(int));
                    offset3 += sizeof(int);
                    if (BitConverter.ToInt32(tmp, 0) != 0)
                    {
                        int kek = BitConverter.ToInt32(tmp, 0);
                        if ((kek >= 1) && (kek <= 7))
                            kek = kek;
                        list.Add(BitConverter.ToInt32(tmp, 0));//!!!
                        blocks.Add(BitConverter.ToInt32(tmp, 0));
                    }
                }
                 offset3 = 0;
                    //обращаемся к адресам и читаем эти блоки
                    for (var j=0;j<list.Count;j++)
                    {
                        int test = list[j];
                        if (list[j] != 0)
                        {
                            byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[j] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);// прочитали блок с адресами информационных блоков
                            //if (data[0]!=0)
                            if ((data[0] != 0) || (data[1] != 0) || (data[2] != 0) || (data[3] != 0))
                            {
                            for (var i = 0; i < 1024; i++)
                            {
                                byte[] tmp = new byte[sizeof(int)];
                                Buffer.BlockCopy(data, offset3, tmp, 0, sizeof(int));
                                offset3 += sizeof(int);
                                int temp = BitConverter.ToInt32(tmp, 0);
                                if (temp!=0)
                                blocks.Add(temp);
                            }
                            offset3 = 0;
                            }
                        }
                    }
                    Inode.BlockPointer[13] = 0;
            }
            for (var i = 0; i < 12; i++)
                Inode.BlockPointer[i] = 0;
            cat Record = new cat();
            Record.Inode_num = record.Inode_num;
            RewriteCatRecord(record.Inode_num, Record);
            Blocks_Bitmap.SetBitmapState(blocks,false);
            InodeBitmap.SetBitmapState(record.Inode_num,false);
            SuperBlock.sb_free_blocks_count = Blocks_Bitmap.FreeBlocks;
            SuperBlock.sb_free_inodes_count = InodeBitmap.FreeBlocks;
           
            int filelength = GetSB().Length + GetBitmap().Length + GetIBitmap().Length;
            Encryption.WriteFile(OpenPath, GetSB(), Constants.BLOCK_SIZE, 0, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetBitmap(), Constants.BLOCK_SIZE, GetSB().Length, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetIBitmap(), Constants.BLOCK_SIZE, GetSB().Length + GetBitmap().Length, FormsVar.Password);
            
            RewriteInode(record.Inode_num, Inode);
            FormsVar.BSize = SuperBlock.sb_free_blocks_count;
        }

        public void OpenFile(cat record,BackgroundWorker worker, DoWorkEventArgs e)
        {
            string PathtoWrite = Path.GetTempPath();
            string name = Encoding.Unicode.GetString(record.Name);
            name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
            ReadFileFromFSlist(PathtoWrite, record);
            string Path2 = PathtoWrite + name;
            ProcessStartInfo psInfo = new ProcessStartInfo();
            //Process myProc2 = new Process();
            //string format = Path.GetExtension(Path2);
            //if ((format == ".jpg") || (format == ".png") || (format == ".jpeg") || (format == ".tiff"))
            //    myProc2=OpenFileCS.OpenPicView(Path2);
            //else
            //if ((format == ".docx") || (format == ".doc"))
            //    myProc2 = OpenFileCS.OpenMicrosoftWord(Path2);
            //else
            //if ((format == ".xls") || (format == ".xlsx"))
            //    myProc2 = OpenFileCS.OpenExcel(Path2);
            //else
            //{
                Process myProc = Process.Start(Path2);
                try
                {
                    myProc.WaitForExit();
                    RemoveFile(record);
                    PathtoWrite += "\\" + name;
                    AddFileUpd(PathtoWrite);
                    File.Delete(PathtoWrite);
                }
                    catch
                {
                    Exception ex = new Exception();
                    System.Windows.Forms.MessageBox.Show("Невозможно безопасно открыть файл");
                    PathtoWrite += "\\" + name;
                    File.Delete(PathtoWrite);
                    worker.ReportProgress(100); 
                }
                worker.ReportProgress(100); 
            //}
            //try
            //{
            //    myProc2.WaitForExit();
            //    RemoveFile(record);
            //    PathtoWrite += "\\" + name;
            //    AddFileUpd(PathtoWrite);
            //    File.Delete(PathtoWrite);
            //}
            //catch
            //{
            //    System.Windows.Forms.MessageBox.Show("Невозможно безопасно открыть файл");
            //    PathtoWrite += "\\" + name;
            //    File.Delete(PathtoWrite);
            //}
            
        }

        public void ReadFileFromFSlist(string PathToWrite,cat record)
        {
            string name = Encoding.Unicode.GetString(record.Name);
            name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
            PathToWrite += "\\" + name;
            int filesize = record.Size;
            int SizeinBlocks = (filesize / Constants.BLOCK_SIZE + (filesize % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            int LastBlockSize = (filesize % Constants.BLOCK_SIZE);
            int fileoffset = 0;
            int offset = 0;
            int currentblock = 0;
            List<int> blocks=new List<int>();
            inode_struct Inode = new inode_struct();
            Inode = GetInode(record.Inode_num);
            //first 12
            for (var i = 0; i < 12; i++)
            {
                currentblock++;
                if (Inode.BlockPointer[i] != 0)
                {
                    blocks.Add(Inode.BlockPointer[i]);
                }
            }
            //13 pointer
            if (SizeinBlocks > 12)
            {
                int offset2 = 0;
                byte[] adrblock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[12] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                for (var i = 0; i < 1024; i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset2, tmp, 0, sizeof(int));
                    offset2 += sizeof(int);
                    if (BitConverter.ToInt32(tmp, 0) != 0)
                        blocks.Add(BitConverter.ToInt32(tmp, 0));
                }
            }
            //14 pointer
            if (SizeinBlocks > 1036)
            {
                int offset3 = 0;
                //read 14 pointer for list of adresses
                byte[] adrblock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[13] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                List<int> list = new List<int>();
                for (var i = 0; i < 1024; i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset3, tmp, 0, sizeof(int));
                    offset3 += sizeof(int);
                    if (BitConverter.ToInt32(tmp, 0) != 0)
                        list.Add(BitConverter.ToInt32(tmp, 0));//!!!
                }
                offset3 = 0;
                //обращаемся к адресам и читаем эти блоки
                for (var j = 0; j < list.Count; j++)
                {
                    int test = list[j];
                    if (list[j] != 0)
                    {
                        byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[j] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);// прочитали блок с адресами информационных блоков
                        if ((data[0] != 0) || (data[1] != 0) || (data[2] != 0) || (data[3] != 0))
                        {
                            for (var i = 0; i < 1024; i++)
                            {
                                byte[] tmp = new byte[sizeof(int)];
                                Buffer.BlockCopy(data, offset3, tmp, 0, sizeof(int));
                                offset3 += sizeof(int);
                                int temp = BitConverter.ToInt32(tmp, 0);
                                if (temp != 0)
                                    blocks.Add(temp);
                            }
                            offset3 = 0;
                        }
                    }
                }
            }
            int counter=0;
            //var isEqual = blocks.Any(x => blockstest.Contains(x));
            //if (isEqual)
                //Console.WriteLine("kek");
            //ReadFileofFS(PathToWrite, blocks, record.Size);
            ReadFileofFSMEM(PathToWrite, blocks, record.Size);
        }

        public void ReadFileFromFS(string PathToWrite,cat record)
        {
            string name = Encoding.Unicode.GetString(record.Name);
            name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
            PathToWrite += "\\"+name;
            int filesize = record.Size;
            //попытка оптимизации скорости чтения
            byte[] file = new byte[filesize];
            //------------------------------------
            int SizeinBlocks = (filesize / Constants.BLOCK_SIZE + (filesize % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            int LastBlockSize = (filesize % Constants.BLOCK_SIZE);
            int fileoffset=0;
            int offset=0;
            int currentblock = 0;
            inode_struct Inode = new inode_struct();
            Inode = GetInode(record.Inode_num);
            //read first 12 pointers
            for (var i = 0; i < 12;i++)
            {
                currentblock++;
                if (Inode.BlockPointer[i] != 0)
                {
                    if ((filesize % Constants.BLOCK_SIZE > 0) && (currentblock == SizeinBlocks))
                    {
                        //byte[] data33 = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode_table[record.Inode_num].BlockPointer[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);                  
                        byte[] data2 = Encryption.ReadFile(OpenPath, filesize % Constants.BLOCK_SIZE, (int)Inode.BlockPointer[i] * Constants.BLOCK_SIZE, filesize % Constants.BLOCK_SIZE, FormsVar.Password);
                        //optimisation
                        //Encryption.WriteFile(PathToWrite, data2, filesize % Constants.BLOCK_SIZE, offset);
                        Buffer.BlockCopy(data2, 0, file, fileoffset, filesize % Constants.BLOCK_SIZE);
                        fileoffset += filesize % Constants.BLOCK_SIZE;
                        offset += data2.Length;
                        Encryption.WriteFile(PathToWrite, file, filesize, 0, FormsVar.Password);
                        //File.WriteAllBytes(PathToWrite, file);
                        break;
                    }
                    byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);                  
                    //Encryption.WriteFile(PathToWrite, data, Constants.BLOCK_SIZE, offset);
                    Buffer.BlockCopy(data, 0, file, fileoffset, data.Length);
                    fileoffset += data.Length;
                    offset += data.Length;
                }
            }
            //if size>12 blocks then read 13 pointer
            if (SizeinBlocks>12)
            {
                //read 13 pointer and get list of addresses
                int offset2 = 0;
                byte[] adrblock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[12] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                List<int> list = new List<int>();
                for (var i=0;i<1024;i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset2, tmp, 0, sizeof(int));
                    offset2 += sizeof(int);
                    if (BitConverter.ToInt32(tmp,0)!=0)
                    list.Add(BitConverter.ToInt32(tmp,0));
                }
                //copy data from addresses
               for (var i=0;i<list.Count;i++)
               {
                   currentblock++;
                   if (list[i] != 0)
                   {
                       if ((filesize % Constants.BLOCK_SIZE > 0) && (currentblock == SizeinBlocks))
                    {
                        int offsetf = list[i] * Constants.BLOCK_SIZE;
                        byte[] data2 = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, offsetf, filesize % Constants.BLOCK_SIZE, FormsVar.Password);
                        //Encryption.WriteFile(PathToWrite, data2, filesize % Constants.BLOCK_SIZE, offset);
                        Buffer.BlockCopy(data2, 0, file, fileoffset, filesize % Constants.BLOCK_SIZE);
                        fileoffset += data2.Length;
                        Encryption.WriteFile(PathToWrite, file, filesize, 0, FormsVar.Password);
                        //File.WriteAllBytes(PathToWrite, file);
                        offset += data2.Length;
                        break;
                    }

                       byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                       //Encryption.WriteFile(PathToWrite, data, Constants.BLOCK_SIZE, offset);
                       Buffer.BlockCopy(data, 0, file, fileoffset, data.Length);
                       fileoffset += data.Length;
                       offset += data.Length;
                   }
                   else break;
               }

            }
            //if size >1036 then Read 14 pointer
            if (SizeinBlocks>1036)
            {
                int offset3 = 0;
                //read 14 pointer for list of adresses
                byte[] adrblock = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode.BlockPointer[13] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                List<int> list = new List<int>();
                for (var i = 0; i < 1024; i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset3, tmp, 0, sizeof(int));
                    offset3 += sizeof(int);
                    if (BitConverter.ToInt32(tmp, 0)!=0)
                        list.Add(BitConverter.ToInt32(tmp, 0));//!!!
                }
                offset3 = 0;
                    //обращаемся к адресам и читаем эти блоки
                    for (var j=0;j<list.Count;j++)
                    {
                        int test = list[j];
                        if (list[j] != 0)
                        {
                            byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[j] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);// прочитали блок с адресами информационных блоков
                            if ((data[0]!=0)||(data[1]!=0)||(data[2]!=0)||(data[3]!=0))
                            {
                            for (var i = 0; i < 1024; i++)
                            {
                                byte[] tmp = new byte[sizeof(int)];
                                Buffer.BlockCopy(data, offset3, tmp, 0, sizeof(int));
                                offset3 += sizeof(int);
                                int temp = BitConverter.ToInt32(tmp, 0);
                                //write block if address !=0
                                if ((temp != 0)&&(temp>0))
                                {
                                    currentblock++;
                                    if (currentblock == 1395)
                                        currentblock = currentblock;
                                    if ((filesize % Constants.BLOCK_SIZE > 0) && (currentblock == SizeinBlocks))
                                    {
                                        byte[] data3 = Encryption.ReadFile(OpenPath, filesize % Constants.BLOCK_SIZE, temp * Constants.BLOCK_SIZE, filesize % Constants.BLOCK_SIZE, FormsVar.Password);
                                        //Encryption.WriteFile(PathToWrite, data3, filesize % Constants.BLOCK_SIZE, offset);
                                        Buffer.BlockCopy(data3, 0, file, fileoffset, filesize % Constants.BLOCK_SIZE);
                                        fileoffset += filesize % Constants.BLOCK_SIZE;
                                        Encryption.WriteFile(PathToWrite, file, filesize, 0, FormsVar.Password);
                                        //File.WriteAllBytes(PathToWrite, file);
                                        offset += data3.Length;
                                        goto Finish;
                                   }

                                    byte[] data2 = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, temp * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                                    //Encryption.WriteFile(PathToWrite, data2, Constants.BLOCK_SIZE, offset);
                                    Buffer.BlockCopy(data2, 0, file, fileoffset, data2.Length);
                                    fileoffset += data2.Length;
                                    if (fileoffset == filesize)
                                    {
                                        Encryption.WriteFile(PathToWrite, file, filesize, 0, FormsVar.Password);
                                        goto Finish;
                                    }
                                    offset += data2.Length;
                                }
                                else break;
                            }
                            offset3 = 0;
                        }
                        else
                            {
                                Encryption.WriteFile(PathToWrite, file, filesize, 0, FormsVar.Password);
                                break; }
                                
                        }
                    }
                    //Encryption.WriteFile(PathToWrite, file, filesize, 0);
                    //File.WriteAllBytes(PathToWrite, file);
            }
            else
                Encryption.WriteFile(PathToWrite, file, filesize, 0, FormsVar.Password);
            FormsVar.BSize = SuperBlock.sb_free_blocks_count;
        Finish: return;
        }

        public void AddFileUpd(string PathToFile)
        {  
            string name2 = String.Empty;
            name2 = Path.GetFileName(PathToFile);
            Random rnd=new Random();
            if (ComparewithFilesMEM(name2))
                name2 = Path.GetFileNameWithoutExtension(PathToFile) + rnd.Next()+ Path.GetExtension(PathToFile);
            byte[] bname = Controller.GetBytes(name2);
            byte[] name = new byte[118];
            Buffer.BlockCopy(bname, 0, name, 0, bname.Length);
            //READ FILE
            //byte[] data = Encryption.ReadFile(PathToFile, Constants.BLOCK_SIZE);
            byte[] data = FileSystemIO.ReadFile(PathToFile);
            //check that file normally writed
            //Encryption.WriteFile("C:\\test\\" + name2, data, data.Length, 0);
            var size = data.Length;
            var SizeinBlocks = (size / Constants.BLOCK_SIZE + (size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            //check that we have free space for this file
            if ((SuperBlock.sb_free_blocks_count < SizeinBlocks)||(SuperBlock.sb_free_inodes_count<0))
            {
                System.Windows.Forms.MessageBox.Show("Не хватает места!");
                return;
            }
            
            
            //start to write file
            List<int> blocks = new List<int>();//var for  free blocks for file
            List<int> Iblocks = new List<int>();//var for free inode
            //selectd blocks for file
            var inodeblock = InodeBitmap.GetFreeBlock();
            inode_struct Inode = new inode_struct();
           
            InodeBitmap.SetBitmapState(inodeblock,true);
            //blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks);
            //blockstest = blocks;
            //Blocks_Bitmap.SetBitmapState(blocks, true);
            Inode.Number = inodeblock;
            Inode.FileSize = size;
            Inode.FileType = 20;
            this.Inode = Inode;
            //До этого момента нужно выделить блоки для записи структур и заблокировать их
            Inode = AddStructures(SizeinBlocks);
            blocks = blockstest;
            cat Record = new cat();
            Record.Name = name;
            Record.Inode_num = inodeblock;
            Record.Size = size;
            Record.Type = 20;
            RewriteCatRecord(Record.Inode_num, Record);
            //AddCatRecord(Record);
            RewriteInode(Record.Inode_num, Inode);
            //write file into container
            WriteFileToFS(OpenPath, blocks, data);
            //banning Bitmap bits
            
            //variables for SIB and DIB
            //TEST
            //ReadFileofFS("C:\\test\\k" + name2, blocks,size);
            
            //addresses writed
            //cat Record = new cat();
            //Record.Name = name;
            //Record.Inode_num = inodeblock;
            //Record.Size = size;
            //Record.Type = 20;
            //Inode.FileSize = size;
            //Inode.FileType = 20;
            //AddCatRecord(Record);
            SuperBlock.sb_free_blocks_count = Blocks_Bitmap.FreeBlocks;
            SuperBlock.sb_free_inodes_count = InodeBitmap.FreeBlocks;

            int filelength = GetSB().Length + GetBitmap().Length + GetIBitmap().Length;
            Encryption.WriteFile(OpenPath, GetSB(), Constants.BLOCK_SIZE, 0, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetBitmap(), Constants.BLOCK_SIZE, GetSB().Length, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetIBitmap(), Constants.BLOCK_SIZE, GetSB().Length + GetBitmap().Length, FormsVar.Password);
            //RewriteInode(Record.Inode_num, Inode);
            FormsVar.BSize = SuperBlock.sb_free_blocks_count;
            //ReadFileFromFSlist("C:\\test\\kk", Record);
            //ReadFileofFS("C:\\test\\kk" + name2, blocks, size);
        }

        public void AddFileUpd(string PathToFile, BackgroundWorker worker, DoWorkEventArgs e)
        {   //testing

            // FILE NAME
            worker.ReportProgress(0);
            string name2 = String.Empty;
            name2 = Path.GetFileName(PathToFile);
            string namewoext = Path.GetFileNameWithoutExtension(PathToFile);
            Random rnd = new Random();
            if (ComparewithFilesMEM(name2))
            {
                name2 = Path.GetFileNameWithoutExtension(PathToFile) + rnd.Next() + Path.GetExtension(PathToFile);
                namewoext = Path.GetFileNameWithoutExtension(name2);
            }
            byte[] bname = Controller.GetBytes(name2);
            byte[] name = new byte[118];
            Buffer.BlockCopy(bname, 0, name, 0, bname.Length);
            //READ FILE
            //byte[] data = Encryption.ReadFile(PathToFile, Constants.BLOCK_SIZE);
            byte[] data = FileSystemIO.ReadFile(PathToFile);
            //check that file normally writed
            //Encryption.WriteFile("C:\\test\\" + name2, data, data.Length, 0);
            var size = data.Length;
            var SizeinBlocks = (size / Constants.BLOCK_SIZE + (size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            worker.ReportProgress(10);
            //check that we have free space for this file
            if ((SuperBlock.sb_free_blocks_count < SizeinBlocks) || (SuperBlock.sb_free_inodes_count < 0))
            {
                System.Windows.Forms.MessageBox.Show("Не хватает места!");
                return;
            }


            //start to write file
            List<int> blocks = new List<int>();//var for  free blocks for file
            List<int> Iblocks = new List<int>();//var for free inode
            //selectd blocks for file
            var inodeblock = InodeBitmap.GetFreeBlock();
            inode_struct Inode = new inode_struct();

            InodeBitmap.SetBitmapState(inodeblock, true);
            //blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks);
            //blockstest = blocks;
            //Blocks_Bitmap.SetBitmapState(blocks, true);
            Inode.Number = inodeblock;
            Inode.FileSize = size;
            Inode.FileType = 20;
            this.Inode = Inode;
            //До этого момента нужно выделить блоки для записи структур и заблокировать их
            Inode = AddStructures(SizeinBlocks);
            worker.ReportProgress(50);
            blocks = blockstest;
            cat Record = new cat();
            Record.Name = name;
            Record.Inode_num = inodeblock;
            Record.Size = size;
            Record.Type = 20;
            RewriteCatRecord(Record.Inode_num, Record);
            worker.ReportProgress(60);
            //AddCatRecord(Record);
            RewriteInode(Record.Inode_num, Inode);
            worker.ReportProgress(70);
            //write file into container

            StringBuilder builder = new StringBuilder();
	        foreach (int safePrime in blocks)
	        {
	         builder.Append(safePrime).Append(" ");
	        }
	        string result = builder.ToString();
            File.WriteAllText("C:\\Users\\igor\\Desktop\\test\\test2\\" + namewoext + ".txt", result);
            WriteFileToFS(OpenPath, blocks, data);
            worker.ReportProgress(90);

            SuperBlock.sb_free_blocks_count = Blocks_Bitmap.FreeBlocks;
            SuperBlock.sb_free_inodes_count = InodeBitmap.FreeBlocks;

            int filelength = GetSB().Length + GetBitmap().Length + GetIBitmap().Length;
            Encryption.WriteFile(OpenPath, GetSB(), Constants.BLOCK_SIZE, 0, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetBitmap(), Constants.BLOCK_SIZE, GetSB().Length, FormsVar.Password);
            Encryption.WriteFile(OpenPath, GetIBitmap(), Constants.BLOCK_SIZE, GetSB().Length + GetBitmap().Length, FormsVar.Password);
            FormsVar.BSize = SuperBlock.sb_free_blocks_count;
            worker.ReportProgress(100);
        }


        public inode_struct AddStructures(int SizeinBlocks)
        {
            int lastwritedblock = 0;
            int blockswrited = 0;
            int items = 0;
            int SIBlock = 0;
            byte[] SIBlockA = new byte[Constants.BLOCK_SIZE];
            List<int> DIBlockS = new List<int>();
            int DIBlock = 0;
            byte[] DIBlockA = new byte[Constants.BLOCK_SIZE];
            //offset variable
            int offset = 0;
            int structblocks = 0;

            if (SizeinBlocks <= 12)
                structblocks = 0;
            
            if (SizeinBlocks > 12)
                  structblocks++;

            if (SizeinBlocks>1036)
            {
                structblocks++;

                if (SizeinBlocks - 1036 <= 1024)
                    structblocks++;
                else
                {
                    int infoblocks = SizeinBlocks - 1036;
                    int inblocks = infoblocks / 1024 + (infoblocks % 1024 > 0 ? 1 : 0);
                    structblocks += inblocks;
                }
            }
            List<int> StructList = new List<int>();
            if (structblocks!=0)
            {
                StructList = Blocks_Bitmap.FreeBlocksIndex(structblocks);
                Blocks_Bitmap.SetBitmapState(StructList, true);
            }
            List<int> blocks = new List<int>();
            blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks);
            Blocks_Bitmap.SetBitmapState(blocks, true);
            blockstest = null;
            blockstest = blocks;
            //starting write first 12 address
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i < 12)
                {
                    Inode.BlockPointer[i] = (int)blocks[i];
                    blockswrited++;
                }
                else break;
            }
            //starting write single indirect block!!!!!!!
            if (SizeinBlocks > 12)
            {
                SIBlock = StructList[items]; //select additional block for address
                //StructList.Remove(StructList[0]);
                items++;
            }
            //add address into block 
            if (blocks.Count > 12)
            {
                for (var i = 12; i < blocks.Count; i++)
                {
                    if (i < 1036)
                    {
                        byte[] temp = BitConverter.GetBytes(blocks[i]);//конвертируем адрес в байтовый массив
                        Buffer.BlockCopy(temp, 0, SIBlockA, offset, sizeof(int));//пишем адрес в блок
                        offset += sizeof(int);
                        lastwritedblock = blocks[i];
                        blockswrited++;
                    }
                }
                offset = 0;
                Encryption.WriteFile(OpenPath, SIBlockA, Constants.BLOCK_SIZE, SIBlock * Constants.BLOCK_SIZE, FormsVar.Password);//write SIBlockA into container
                Inode.BlockPointer[12] = (int)SIBlock;
            }
            //starting write double indirect block!!!!!!!
            if (SizeinBlocks > 1036)
            {
                DIBlock = StructList[items];//address for SIB
                items++;
                //Blocks_Bitmap.SetBitmapState(DIBlock, true);
                if (SizeinBlocks - blockswrited <= 1024)
                {
                    DIBlockS.Add(StructList[items]);//select  blocks for SIB  address blocks
                    items++;
                }
                else
                {
                    int infoblocks = SizeinBlocks - blockswrited;
                    int inblocks = infoblocks / 1024 + (infoblocks % 1024 > 0 ? 1 : 0);
                    for (var i = 0; i < inblocks; i++)
                        DIBlockS.Add(StructList[items+i]);
                }
                //Blocks_Bitmap.SetBitmapState(DIBlockS, true);
                //Blocks_Bitmap.FreeBlocks -= (1 + DIBlockS.Count); 

                //зпписываем адреса блоков с адресами 
                for (var i = 0; i < DIBlockS.Count; i++)
                {
                    byte[] temp = BitConverter.GetBytes(DIBlockS[i]);//конвертируем адрес в байтовый массив
                    Buffer.BlockCopy(temp, 0, DIBlockA, offset, sizeof(int));//пишем адрес в блок
                    offset += sizeof(int);
                }
                offset = 0;
                Encryption.WriteFile(OpenPath, DIBlockA, Constants.BLOCK_SIZE, DIBlock * Constants.BLOCK_SIZE, FormsVar.Password);//write DIBlockA into container
                //write data addresses into SIBlocks
            }
            //записываем адреса информационных блоков в блоки с адресами
            if (blocks.Count > 1036)
            {
                //for (var i=1037;i<blocks.Count;i++)
                int iadr = 1037;
                while (iadr < blocks.Count - 1)
                {
                    if (iadr < 1048588)
                    {
                        for (var j = 0; j < DIBlockS.Count; j++)
                        {
                            byte[] tempres = new byte[Constants.BLOCK_SIZE];
                            if (iadr >= blocks.Count - 1)
                                break;
                            //заполняем отдельно каждый блок адресов (3)
                                for (var k = 0; k < 1024; k++)
                                {
                                int test = blocks[iadr - 1];
                                byte[] temp = BitConverter.GetBytes(blocks[iadr - 1]);//конвертируем адрес в байтовый массив
                                int test2 = BitConverter.ToInt32(temp, 0);
                                Buffer.BlockCopy(temp, 0, tempres, offset, sizeof(int));//пишем адрес в блок
                                offset += sizeof(int);
                                blockswrited++;
                                if (iadr < blocks.Count)
                                    iadr++;
                                else break;
                                }
                            offset = 0;
                            Encryption.WriteFile(OpenPath, tempres, Constants.BLOCK_SIZE, DIBlockS[j] * Constants.BLOCK_SIZE, FormsVar.Password);//write block with blocks[] adresses into container
                        }
                    }
                }
                Inode.BlockPointer[13] = (int)DIBlock;
            }
            return Inode;
        }
        #region Ускоренные варианты работы(за счет ОЗУ)
        public List<string> ReadFilesMEM(BackgroundWorker worker, DoWorkEventArgs e)
        {
            List<string> names = new List<string>();
            List<cat> catalog = new List<cat>();
            int max = 0;
            int count = (((InodeBitmap.TotalBlocks * 128) + (SuperBlock.sb_inodes_map_index * Constants.BLOCK_SIZE)) / Constants.BLOCK_SIZE) + SuperBlock.sb_inodes_map_index;
            if (InodeBitmap.TotalBlocks < 32768)
                max = InodeBitmap.TotalBlocks;
            else
                max = 32768;
            worker.ReportProgress(10);
            int jj = 0;
            var kk = 0.0;
            byte[] bigdata = new byte[4194304];
            bigdata = Encryption.ReadFile(OpenPath, 128, count * Constants.BLOCK_SIZE, bigdata.Length, FormsVar.Password);
            byte[] bcat = new byte[Constants.CAT_SIZE];
            for (var i = 0; i < max; i++)
            {
                int block = i * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
                //Inode = GetInode(i);
                Buffer.BlockCopy(bigdata, i * Constants.CAT_SIZE, bcat, 0, Constants.CAT_SIZE);
                //byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE);
                cat temp = new cat();
                temp.Set(bcat);
                if (temp.Type == 20)
                {
                    catalog.Add(temp);
                }
            }
            worker.ReportProgress(100);
            if (catalog.Count == 0)
            {
                names.Add("Контейнер пуст");
                return names;
            }

            for (var j = 0; j < catalog.Count; j++)
            {
                string name = Encoding.Unicode.GetString(catalog[j].Name);
                name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
                if (name != "")
                    names.Add(Encoding.Unicode.GetString(catalog[j].Name));
            }
            return names;
        }

        public cat ReadFilesMEM(string CompareName)
        {
            List<string> names = new List<string>();
            List<cat> catalog = new List<cat>();
            int max = 0;
            int count = (((InodeBitmap.TotalBlocks * 128) + (SuperBlock.sb_inodes_map_index * Constants.BLOCK_SIZE)) / Constants.BLOCK_SIZE) + SuperBlock.sb_inodes_map_index;
            if (InodeBitmap.TotalBlocks < 32768)
                max = InodeBitmap.TotalBlocks;
            else
                max = 32768;
            byte[] bigdata = new byte[4194304];
            bigdata = Encryption.ReadFile(OpenPath, bigdata.Length, count * Constants.BLOCK_SIZE, bigdata.Length, FormsVar.Password);
            byte[] bcat = new byte[Constants.CAT_SIZE];
            for (var i = 0; i < max; i++)
            {
                int block = i * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
                //Inode = GetInode(i);
                Buffer.BlockCopy(bigdata, i * Constants.CAT_SIZE, bcat, 0, Constants.CAT_SIZE);
                //byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE);
                cat temp = new cat();
                temp.Set(bcat);
                if (temp.Type == 20)
                {
                    catalog.Add(temp);
                }  
            }
            if (catalog.Count == 0)
            {
                names.Add("Контейнер пуст");
                return null;
            }

            for (var j = 0; j < catalog.Count; j++)
            {
                if (Encoding.Unicode.GetString(catalog[j].Name) == CompareName)
                {
                    return catalog[j];
                }
            }
            return null;
            
        }

        public bool ComparewithFilesMEM(string CompareName)
        {
            List<string> names = new List<string>();
            List<cat> catalog = new List<cat>();
            int max = 0;
            int count = (((InodeBitmap.TotalBlocks * 128) + (SuperBlock.sb_inodes_map_index * Constants.BLOCK_SIZE)) / Constants.BLOCK_SIZE) + SuperBlock.sb_inodes_map_index;
            if (InodeBitmap.TotalBlocks < 32768)
                max = InodeBitmap.TotalBlocks;
            else
                max = 32768;
            byte[] bigdata = new byte[4194304];
            bigdata = Encryption.ReadFile(OpenPath, bigdata.Length, count * Constants.BLOCK_SIZE, bigdata.Length, FormsVar.Password);
            byte[] bcat = new byte[Constants.CAT_SIZE];
            for (var i = 0; i < max; i++)
            {
                int block = i * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
                //Inode = GetInode(i);
                    Buffer.BlockCopy(bigdata, i * Constants.CAT_SIZE, bcat, 0, Constants.CAT_SIZE);
                    //byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE);
                    cat temp = new cat();
                    temp.Set(bcat);
                    if (temp.Type == 20)
                    {
                    catalog.Add(temp);
                    }   
            }
            if (catalog.Count == 0)
            {
                names.Add("Контейнер пуст");
                return false;
            }

            for (var j = 0; j < catalog.Count; j++)
            {
                string name = Encoding.Unicode.GetString(catalog[j].Name);
                name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
                if (name == CompareName)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Медленные варианты, но надежные
        public List<string> ReadFiles()
        {
            List<string> names = new List<string>();
            List<int> inodes = new List<int>();
            List<cat> catalog = new List<cat>();
            inode_struct Inode = new inode_struct();
            int max = 0;
            int count = (((InodeBitmap.TotalBlocks * 128) + (SuperBlock.sb_inodes_map_index * Constants.BLOCK_SIZE)) / Constants.BLOCK_SIZE) + SuperBlock.sb_inodes_map_index;
            if (InodeBitmap.TotalBlocks < 32768)
                max = InodeBitmap.TotalBlocks;
            else
                max = 32768;
            for (var i=0;i<max;i++)
            {
                int block = i * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
                Inode = GetInode(i);
                if (Inode.FileType==20)
                {
                    byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE, FormsVar.Password);
                    cat temp = new cat();
                    temp.Set(data);
                    catalog.Add(temp);
                }
            }
            if (catalog.Count==0)
            {
                    names.Add("Контейнер пуст");
                    return names;
            }

                for (var j = 0; j < catalog.Count; j++)
                {
                    string name = Encoding.Unicode.GetString(catalog[j].Name);
                    name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
                    if (name != "")
                        names.Add(Encoding.Unicode.GetString(catalog[j].Name));
                }
                return names;
       }


        public cat ReadFiles(string CompareName)
        {
            List<string> names = new List<string>();
            List<int> inodes = new List<int>();
            List<cat> catalog = new List<cat>();
            inode_struct Inode = new inode_struct();
            int max = 0;
            int count = (((InodeBitmap.TotalBlocks * 128) + (SuperBlock.sb_inodes_map_index * Constants.BLOCK_SIZE)) / Constants.BLOCK_SIZE) + SuperBlock.sb_inodes_map_index;
            if (InodeBitmap.TotalBlocks < 32768)
                max = InodeBitmap.TotalBlocks;
            else
                max = 32768;
            for (var i = 0; i < max; i++)
            {
                int block = i * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
                Inode = GetInode(i);
                if (Inode.FileType == 20)
                {
                    byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE, FormsVar.Password);
                    cat temp = new cat();
                    temp.Set(data);
                    catalog.Add(temp);
                }
            }
            if (catalog.Count == 0)
            {
                names.Add("Контейнер пуст");
                return null;
            }
           for (var j=0;j<catalog.Count;j++)
           {
               if (Encoding.Unicode.GetString(catalog[j].Name)==CompareName)
               {
                   return catalog[j];
               }
           }
           return null;
        }

        public bool ComparewithFiles(string CompareName)
        {
            List<string> names = new List<string>();
            List<int> inodes = new List<int>();
            List<cat> catalog = new List<cat>();
            inode_struct Inode = new inode_struct();
            int max = 0;
            int count = (((InodeBitmap.TotalBlocks * 128) + (SuperBlock.sb_inodes_map_index * Constants.BLOCK_SIZE)) / Constants.BLOCK_SIZE) + SuperBlock.sb_inodes_map_index;
            if (InodeBitmap.TotalBlocks < 32768)
                max = InodeBitmap.TotalBlocks;
            else
                max = 32768;
            for (var i = 0; i < max; i++)
            {
                int block = i * Constants.CAT_SIZE + count * Constants.BLOCK_SIZE;
                Inode = GetInode(i);
                if (Inode.FileType == 20)
                {
                    byte[] data = Encryption.ReadFile(OpenPath, Constants.CAT_SIZE, block, Constants.CAT_SIZE, FormsVar.Password);
                    cat temp = new cat();
                    temp.Set(data);
                    catalog.Add(temp);
                }
            }

            if (catalog.Count == 0)
            {
                names.Add("Контейнер пуст");
                return false;
            }
            
            for (var j = 0; j < catalog.Count; j++)
            {
                string name = Encoding.Unicode.GetString(catalog[j].Name);
                name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
                if (name == CompareName)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
        private void WriteFileToFS(string Path,List<int> list,byte[] data)
        {
            int offset = 0;
            bool check = false;
            int size = list.Count;
            byte[] extend = new byte[Constants.BLOCK_SIZE];
            if (data.Length<Constants.BLOCK_SIZE)
            {
                for (var i = 0; i < data.Length;i++ )
                    extend[i] = data[i];
                data = extend;
            }
            if (data.Length % Constants.BLOCK_SIZE!=0)
            {
                size--;
            }
            byte[] part = new byte[Constants.BLOCK_SIZE];
            for (var i = 0; i < size;i++)
            {
                Buffer.BlockCopy(data, offset, part, 0, part.Length);
                offset += part.Length;
                Encryption.WriteFile(Path, part, Constants.BLOCK_SIZE, list[i] * Constants.BLOCK_SIZE, FormsVar.Password);
            }
            if (data.Length % Constants.BLOCK_SIZE != 0)
            {
                byte[] temp = new byte[data.Length % Constants.BLOCK_SIZE];
                Buffer.BlockCopy(data, offset, temp, 0, temp.Length);
                for (var i = 0; i < temp.Length; i++)
                    extend[i] = temp[i];
                //Encryption.WriteFile(Path, extend, Constants.BLOCK_SIZE, list[size] * Constants.BLOCK_SIZE);
                Encryption.WriteFile(Path, temp, temp.Length, list[size] * Constants.BLOCK_SIZE, FormsVar.Password);
            }     
        }



        private void ReadFileofFS(string Path, List<int> list,int filesize)
        {
            int offset = 0;
            int size = list.Count;
            bool check = false;
            if (filesize%Constants.BLOCK_SIZE!=0)
            {
                size--;
                check = true;
            }
            for (var i = 0; i < size; i++)
            {
                byte[] data = new byte[Constants.BLOCK_SIZE];
                data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                Encryption.WriteFile(Path, data, Constants.BLOCK_SIZE, offset, FormsVar.Password);
                offset += data.Length;
            }
            if (check)
            {
                offset = Constants.BLOCK_SIZE * (list.Count - 1);
                byte[] lastdata = new byte[filesize % Constants.BLOCK_SIZE];
                int lastsize = lastdata.Length;
                lastdata = Encryption.ReadFile(OpenPath, lastsize, list[list.Count - 1] * Constants.BLOCK_SIZE, lastsize, FormsVar.Password);
                Encryption.WriteFile(Path, lastdata, lastsize, offset, FormsVar.Password);
            }
            
        }

        private void ReadFileofFSMEM(string Path, List<int> list, int filesize)
        {
            int offset = 0;
            byte[] bigdata = new byte[filesize];
            int size = list.Count;
            bool check = false;
            if (filesize % Constants.BLOCK_SIZE != 0)
            {
                if (filesize - filesize % Constants.BLOCK_SIZE==0)
                {
                    check = true;
                    goto SmallFile;
                }
                bigdata = new byte[filesize - filesize % Constants.BLOCK_SIZE];
                size--;
                check = true;
            }
            for (var i = 0; i < size; i++)
            {
                byte[] data = new byte[Constants.BLOCK_SIZE];
                data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
                Buffer.BlockCopy(data, 0, bigdata, offset, data.Length);
                //Encryption.WriteFile(Path, data, Constants.BLOCK_SIZE, offset);
                offset += data.Length;
            }
            FileSystemIO.WriteFile(Path, bigdata, bigdata.Length, 0, FormsVar.Password);
            SmallFile:
            if (check)
            {
                offset = Constants.BLOCK_SIZE * (list.Count - 1);
                byte[] lastdata = new byte[filesize % Constants.BLOCK_SIZE];
                int lastsize = lastdata.Length;
                lastdata = Encryption.ReadFile(OpenPath, lastsize, list[list.Count - 1] * Constants.BLOCK_SIZE, lastsize, FormsVar.Password);
                FileSystemIO.WriteFile(Path, lastdata, lastsize, offset, FormsVar.Password);
            }

        }

        private List<int> ReadAddressBlock(int block)
        {
            List<int> result = new List<int>();
            int offset = 0;
            byte[] data = Encryption.ReadFile(OpenPath, Constants.BLOCK_SIZE, block * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, FormsVar.Password);
            for (var i = 0; i < Marshal.SizeOf(data) / sizeof(int); i++)
            {
                byte[] part = new byte[Constants.CAT_SIZE];
                Buffer.BlockCopy(data, offset, part, 0, part.Length);
                offset += part.Length;
                int tmp = BitConverter.ToInt32(part, 0);
                result.Add(tmp);
            }
            return result;
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

    }

}


