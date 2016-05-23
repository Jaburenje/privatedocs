using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
       public inode_struct[] Inode_table;
       public string OpenPath;
    public Controller(string Path)
    {
    SuperBlock=new SB();
    InodeBitmap = null;
    Blocks_Bitmap = null;
    Inode = null;
    Inode_table = null;
    OpenPath = Path;
    }
        /// <summary>
        /// Создание контейнера
        /// </summary>
        /// <param name="ContainerSize">РАЗМЕР В БАЙТАХ</param>
        public void CreateSBManual(int ContainerSize)
        {
            SuperBlock.sb_blocks_count = ContainerSize / Constants.BLOCK_SIZE;
            SuperBlock.sb_block_size = Constants.BLOCK_SIZE;
            SuperBlock.sb_check_time = Constants.CHECK_INTERVAL;
            SuperBlock.sb_free_blocks_count = SuperBlock.sb_blocks_count - 1;
        }
        public void ReadServiceInfo(byte[] SBlock)
        {
            SB ReadedSB = new SB(SBlock);
            SuperBlock = ReadedSB;
            byte[] BBlock = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE, (SuperBlock.sb_block_map_index - 1) * Constants.BLOCK_SIZE);
            Bitmap_Blocks BB = new Bitmap_Blocks(BBlock);
            Blocks_Bitmap = BB;
            byte[] IBlock = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE * SuperBlock.sb_block_map_index, (SuperBlock.sb_inodes_map_index - SuperBlock.sb_block_map_index) * Constants.BLOCK_SIZE);
            Bitmap_Blocks IB = new Bitmap_Blocks(IBlock);
            InodeBitmap = IB;
            byte[] ind = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE * (SuperBlock.sb_inodes_map_index), Constants.BLOCK_SIZE);
            Inode = new inode_struct(0);
            Inode.Set(ind);

            var offset=0;
            byte[] ITable = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE * SuperBlock.sb_inodes_map_index, (SuperBlock.sb_first_data_block-SuperBlock.sb_inodes_map_index-SuperBlock.sb_block_map_index-1) * Constants.BLOCK_SIZE);
            this.Inode_table = new inode_struct[InodeBitmap.TotalBlocks];
            for (var i = 0; i < InodeBitmap.TotalBlocks;i++ )
            {
                byte[] tmp = new byte[Constants.INODE_SIZE];
                Buffer.BlockCopy(ITable, offset, tmp, 0, tmp.Length);
                offset += tmp.Length;
                this.Inode_table[i] = new inode_struct();

                this.Inode_table[i].Set(tmp);
            }

        }

        public byte[] CreateServiceInfo()
        {
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
            //чтобы ровно все подсчитать, сделаем предрасчет сколько займут структуры 1 структура 128байт
            int inode_struct_size = ((inodes_count * Constants.INODE_SIZE) / Constants.BLOCK_SIZE) + ((inodes_count * Constants.INODE_SIZE) % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            inodes_count -= inode_struct_size;
            InodeBitmap = new Bitmap_Blocks(inodes_count);
            ib_size = InodeBitmap.GetSize();
            ib_bl_size = (ib_size / Constants.BLOCK_SIZE + (ib_size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
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
            //var real_inodes_count=((Inodes.Length*Inodes[0].GetSize())/Constants.BLOCK_SIZE)+(((Inodes.Length*Inodes[0].GetSize())%Constants.BLOCK_SIZE)>0?1:0);//размер в блоках
            //inodes_count -= real_inodes_count;
            //памяти реально выделено больше, чем нужно, так что нужно корректировать это все

            #region BitmapFilling
            var filledBlocks = (bbl_size + ib_bl_size + inode_struct_size+1)/Constants.BIT_IN_BYTE_COUNT;//тут карочи берем количество блоков и делим их на 8(биты в байтах) чтобы занять байтовую переменную правильно при заполнении битмапа
            int mod = (bbl_size + ib_bl_size + inode_struct_size + 1) % Constants.BIT_IN_BYTE_COUNT;
            //var abc = BlocksBitmap.GetAllBlocksCount() - blocksCount; // разница в битах между тем, сколько реально выделено и сколько надо было (9 блоков = 2 байта)
            //var byteIndex = BlocksBitmap.Map.Length - 1;
            //BlocksBitmap.Map[byteIndex] = Convert.ToByte(Math.Pow(2, FSProps.BIT_IN_BYTE_COUNT) - Math.Pow(2, FSProps.BIT_IN_BYTE_COUNT - abc));

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
            
            #region InodeBitmap 
            //в битоовй карте i-узлов не заполнено ничего, т.к. нет стартовых файлов и фс пустая
            //filledBlocks = (inodes_count + ib_bl_size) / Constants.BIT_IN_BYTE_COUNT;
            //mod = inodes_count % Constants.BIT_IN_BYTE_COUNT;

            //if (mod>=Constants.BIT_IN_BYTE_COUNT)
            //{
            //    filledBlocks++;
            //    mod -= Constants.BIT_IN_BYTE_COUNT;
            //}

            //for (var i=InodeBitmap.indexMap.Length-1;i>=InodeBitmap.indexMap.Length-filledBlocks;--i)
            //{
            //    InodeBitmap.indexMap[i] = 255;
            //    InodeBitmap.FreeBlocks -= Constants.BIT_IN_BYTE_COUNT;
            //}

            //if (mod>0)
            //{
            //    InodeBitmap.indexMap[InodeBitmap.indexMap.Length - filledBlocks - 1] = Convert.ToByte(Math.Pow(2, mod) - 1);
            //    InodeBitmap.FreeBlocks -= mod;
            //}
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
            Inode = CreateRootDirectory(Inodes[0]);
            Inodes[0] = Inode;
            cat[] catRecords = new cat[Constants.BLOCK_SIZE / Constants.INODE_SIZE];
            for (int i=0;i<catRecords.Length;i++)
            {
                catRecords[i] = new cat();
            }
            byte[] byteroot = new byte[Constants.BLOCK_SIZE];
            var offset = 0;
            foreach (cat c in catRecords)
            {
                byte[] block = c.Get();
                Buffer.BlockCopy(block, 0, byteroot, offset, block.Length);
                offset += block.Length;
            }
                
            byte[] result = Assembly(GetSB(), GetBitmap(), GetIBitmap(), GetINodes(Inodes), byteroot);
            return result;
        }
        /// <summary>
        /// Сборщик сервисных блоков фс в байтовый массив
        /// </summary>
        /// <returns></returns>
        public static byte[] Assembly(params byte[][] blocks)
        {
            var size = 0;
            foreach (byte[] block in blocks)
            {
                size += block.Length;
            }

            byte[] result = new byte[size];
            var offset = 0;

            foreach (byte[] block in blocks)
            {
                Buffer.BlockCopy(block, 0, result, offset, block.Length);
                offset += block.Length;
            }

            return result;
        }


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

        public byte[] GetINodes(inode_struct[] inodes)
        {
            byte[] In_arr = new byte[inodes.Length * inodes[0].GetSize()];
            byte[] inode = null;
            var offset = 0;
            foreach(inode_struct ind in inodes)
            {
                inode = ind.Get();
                //check
                inode_struct test = new inode_struct(inode);
                Buffer.BlockCopy(inode, 0, In_arr, offset, inode.Length);
                offset += inode.Length;
            }
            var block_count = (In_arr.Length / Constants.BLOCK_SIZE) + (In_arr.Length % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            byte[] result = new byte[block_count * Constants.BLOCK_SIZE];//ошибка тут
            Buffer.BlockCopy(In_arr, 0, result, 0, In_arr.Length);

            return result;
        }
        
        public inode_struct CreateRootDirectory(inode_struct root)
        {
            root.Set(0,10, 0, new uint[Constants.INODE_BLOCKS]);
            uint freeblock = (uint)Blocks_Bitmap.GetFreeBlock();// заняли блок
            uint iblock = (uint)InodeBitmap.GetFreeBlock();
            root.Number = (int)iblock;
            Debug.Assert(!freeblock.Equals(-1));
            root.BlockPointer[0] = freeblock;
            Blocks_Bitmap.SetBitmapState((int)root.BlockPointer[0], true);
            SuperBlock.sb_free_blocks_count -= 1;
            InodeBitmap.SetBitmapState((int)root.Number,true);
            SuperBlock.sb_free_inodes_count -= 1;
            return root;
        }

        
        public List<cat> ReadCatRecords(int block)
        {
            List<cat> result = new List<cat>();
            int offset=0;
            byte[] data = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE,block*Constants.BLOCK_SIZE,Constants.BLOCK_SIZE);
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

        public List<cat> ReadCatRecords(uint block)
        {
            List<cat> result = new List<cat>();
            int offset = 0;
            byte[] data = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)block * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
            for (var i = 0; i < data.Length / Constants.CAT_SIZE; i++)
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



        private void ReWriteCatRecord(int block,cat record)
        {
            byte[] data = record.Get();
            FileSystemIO.WriteFile(OpenPath, data, Constants.CAT_SIZE, block * Constants.BLOCK_SIZE);
        }

        public void ReadFileFromFS(string PathToWrite,cat record)
        {
            string name = Encoding.Unicode.GetString(record.Name);
            name = name.Remove(name.IndexOf("\0"), name.Length - name.IndexOf("\0"));
            PathToWrite += name;
            int filesize = record.Size;
            int SizeinBlocks = (filesize / Constants.BLOCK_SIZE + (filesize % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            int LastBlockSize = (filesize % Constants.BLOCK_SIZE);
            int offset=0;
            int currentblock = 0;
            //read first 12 pointers
            for (var i = 0; i < 12;i++)
            {
                currentblock++;
                if (Inode_table[record.Inode_num].BlockPointer[i]!=0)
                {
                    if ((filesize % Constants.BLOCK_SIZE > 0) && (currentblock == SizeinBlocks))
                    {
                        //byte[] data33 = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode_table[record.Inode_num].BlockPointer[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);                  
                        byte[] data2 = FileSystemIO.ReadFile(OpenPath, filesize % Constants.BLOCK_SIZE, (int)Inode_table[record.Inode_num].BlockPointer[i] * Constants.BLOCK_SIZE, filesize % Constants.BLOCK_SIZE);
                        FileSystemIO.WriteFile(PathToWrite, data2, filesize % Constants.BLOCK_SIZE, offset);
                        offset += data2.Length;
                        break;
                    }
                    byte[] data = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode_table[record.Inode_num].BlockPointer[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);                  
                    FileSystemIO.WriteFile(PathToWrite, data, Constants.BLOCK_SIZE, offset);
                    offset += data.Length;
                }
            }
            //if size>12 blocks then read 13 pointer
            if (SizeinBlocks>12)
            {
                //read 13 pointer and get list of addresses
                int offset2 = 0;
                byte[] adrblock = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode_table[record.Inode_num].BlockPointer[12] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
                List<int> list = new List<int>();
                for (var i=0;i<1024;i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset2, tmp, 0, sizeof(int));
                    offset2 += sizeof(int);
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
                        byte[] data2 = FileSystemIO.ReadFile(OpenPath,Constants.BLOCK_SIZE, offsetf, filesize % Constants.BLOCK_SIZE);
                        FileSystemIO.WriteFile(PathToWrite, data2, filesize % Constants.BLOCK_SIZE, offset);
                        offset += data2.Length;
                        break;
                    }

                       byte[] data = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[i] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
                       FileSystemIO.WriteFile(PathToWrite, data, Constants.BLOCK_SIZE, offset);
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
                byte[] adrblock = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, (int)Inode_table[record.Inode_num].BlockPointer[13] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
                List<int> list = new List<int>();
                for (var i = 0; i < 1024; i++)
                {
                    byte[] tmp = new byte[sizeof(int)];
                    Buffer.BlockCopy(adrblock, offset3, tmp, 0, sizeof(int));
                    offset3 += sizeof(int);
                    list.Add(BitConverter.ToInt32(tmp, 0));
                }
                offset3 = 0;
                    //обращаемся к адресам и читаем эти блоки
                    for (var j=0;j<list.Count;j++)
                    {
                        int test = list[j];
                        if (list[j] != 0)
                        {
                            byte[] data = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, list[j] * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);// прочитали блок с адресами информационных блоков
                            if (data[0]!=0)
                            {
                            for (var i = 0; i < 1024; i++)
                            {
                                byte[] tmp = new byte[sizeof(int)];
                                Buffer.BlockCopy(data, offset3, tmp, 0, sizeof(int));
                                offset3 += sizeof(int);
                                int temp = BitConverter.ToInt32(tmp, 0);
                                //write block if address !=0
                                if (temp != 0)
                                {
                                    currentblock++;
                                    if (currentblock == 1395)
                                        currentblock = currentblock;
                                    if ((filesize % Constants.BLOCK_SIZE > 0) && (currentblock == SizeinBlocks))
                                    {
                                        byte[] data3 = FileSystemIO.ReadFile(OpenPath, filesize % Constants.BLOCK_SIZE, temp * Constants.BLOCK_SIZE, filesize % Constants.BLOCK_SIZE);
                                        FileSystemIO.WriteFile(PathToWrite, data3, filesize % Constants.BLOCK_SIZE, offset);
                                        offset += data3.Length;
                                        goto Finish;
                                   }

                                    byte[] data2 = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, temp * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
                                    FileSystemIO.WriteFile(PathToWrite, data2, Constants.BLOCK_SIZE, offset);
                                    offset += data2.Length;
                                }
                                else break;
                            }
                            offset3 = 0;
                        }
                        else break;
                        }
                    }

            }
        Finish: return;
        }

        public void AddFileUpd(string PathToFile)
        {   //testing
            int lastwritedblock = 0;
            int blockswrited = 0;
            // FILE NAME
            string name2 = String.Empty;
            name2 = Path.GetFileName(PathToFile);
            byte[] name = Controller.GetBytes(name2);      
            //READ FILE
            //byte[] data = FileSystemIO.ReadFile(PathToFile, Constants.BLOCK_SIZE);
            byte[] data = FileSystemIO.ReadFile(PathToFile);
            //check that file normally writed
            //FileSystemIO.WriteFile("C:\\test\\" + name2, data, data.Length, 0);
            var size = data.Length;
            var SizeinBlocks = (size / Constants.BLOCK_SIZE + (size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            //check that we have free space for this file
            Debug.Assert(SuperBlock.sb_free_blocks_count > SizeinBlocks);
            Debug.Assert(SuperBlock.sb_free_inodes_count > SizeinBlocks);
            //start to write file
            List<int> blocks = new List<int>();//var for  free blocks for file
            List<int> Iblocks = new List<int>();//var for free inode
            //selectd blocks for file
            var inodeblock = InodeBitmap.GetFreeBlock();
            InodeBitmap.SetBitmapState(inodeblock,true);
            blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks);
            //write file into container
            WriteFileToFS(OpenPath, blocks, data);
                //banning Bitmap bits
                Blocks_Bitmap.SetBitmapState(blocks, true);
            //variables for SIB and DIB
            int SIBlock=0;
            byte[] SIBlockA = new byte[Constants.BLOCK_SIZE];
            List<int> DIBlockS = new List<int>();
            int DIBlock = 0;
            byte[] DIBlockA = new byte[Constants.BLOCK_SIZE];
            //offset variable
            int offset = 0;
            //starting write first 12 address
             for (var i=0;i<blocks.Count;i++)
             { 
                 if (i<12)
                 {
                     Inode_table[inodeblock].BlockPointer[i] = (uint)blocks[i];
                     blockswrited++;
                 }
                 else break;
             }
            //starting write single indirect block!!!!!!!
            if (SizeinBlocks>12)
            {
                List<int> bugged = new List<int>();
                bugged = Blocks_Bitmap.FreeBlocksIndex(1);
                SIBlock = bugged[0]; //select additional block for address
                Blocks_Bitmap.SetBitmapState(SIBlock, true);
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
                FileSystemIO.WriteFile(OpenPath, SIBlockA, Constants.BLOCK_SIZE, SIBlock * Constants.BLOCK_SIZE);//write SIBlockA into container
                Inode_table[inodeblock].BlockPointer[12] = (uint)SIBlock;
            }
            //starting write double indirect block!!!!!!!
            if (SizeinBlocks>1036)
                {
                    DIBlock = Blocks_Bitmap.GetFreeBlock();//address for SIB
                    Blocks_Bitmap.SetBitmapState(DIBlock,true);
                    DIBlockS = Blocks_Bitmap.FreeBlocksIndex(1024);//select  blocks for SIB  address blocks
                    Blocks_Bitmap.SetBitmapState(DIBlockS, true);
                    Blocks_Bitmap.FreeBlocks -= (1 + DIBlockS.Count); 
                
            //add addresses of SIBlocks
            for (var i=0;i<DIBlockS.Count;i++)
            {
                byte[] temp = BitConverter.GetBytes(DIBlockS[i]);//конвертируем адрес в байтовый массив
                Buffer.BlockCopy(temp, 0, DIBlockA, offset, sizeof(int));//пишем адрес в блок
                offset += sizeof(int);
            }
            offset = 0;
            FileSystemIO.WriteFile(OpenPath, DIBlockA, Constants.BLOCK_SIZE, DIBlock * Constants.BLOCK_SIZE);//write DIBlockA into container
            //write data addresses into SIBlocks
                }
            if (blocks.Count>1036)
            {
                //for (var i=1037;i<blocks.Count;i++)
                int iadr = 1037;
                while (iadr < blocks.Count-1)
                {
                    if (iadr < 1048588)
                    {
                        for (var j = 0; j < DIBlockS.Count; j++)
                        {
                            byte[] tempres = new byte[Constants.BLOCK_SIZE];
                            if (iadr >= blocks.Count-1)    
                            break;
                            //заполняем отдельно каждый блок адресов (3)
                            for (var k = 0; k < 1024; k++)
                            {
                                int test = blocks[iadr-1];
                                byte[] temp = BitConverter.GetBytes(blocks[iadr-1]);//конвертируем адрес в байтовый массив
                                int test2 = BitConverter.ToInt32(temp,0);
                                Buffer.BlockCopy(temp, 0, tempres, offset, sizeof(int));//пишем адрес в блок
                                offset += sizeof(int);
                                blockswrited++;
                                if (iadr < blocks.Count)
                                    iadr++;
                                else break;
                            }
                            offset = 0;
                            FileSystemIO.WriteFile(OpenPath, tempres, Constants.BLOCK_SIZE, DIBlockS[j] * Constants.BLOCK_SIZE);//write block with blocks[] adresses into container
                        }
                    }
                }
                Inode_table[inodeblock].BlockPointer[13] = (uint)DIBlock;
            }
            //addresses writed
            cat Record = new cat();
            Record.Name = name;
            Record.Inode_num = inodeblock;
            Record.Size = size;
            Record.Type = 20;

            AddCatRecord(Record);
            SuperBlock.sb_free_blocks_count = Blocks_Bitmap.FreeBlocks;
            SuperBlock.sb_free_inodes_count = InodeBitmap.FreeBlocks;
            byte[] service = Assembly(GetSB(), GetBitmap(), GetIBitmap(), GetINodes(this.Inode_table));
            FileSystemIO.WriteFile(OpenPath, service, Constants.BLOCK_SIZE, 0);
    
        }
      
        public List<string> ReadFiles()
        {
            List<string> names = new List<string>();
            List<int> inodes = new List<int>();
            List<cat> catalog = ReadCatRecords(Inode_table[0].BlockPointer[0]);

            int i = 0;
            while (i<catalog.Count)
            {
                if (catalog[i].Inode_num == 0)
                {
                    if (i == 0)
                    {
                        names.Add("Контейнер пуст");
                        return names;
                    }
                    catalog.RemoveAt(i);
                    i -= 1;
                }
                else i++;
            }

            for (var j=0;j<catalog.Count;j++)
            {
                inodes.Add(catalog[j].Inode_num);
                string name = Encoding.Unicode.GetString(catalog[j].Name);
                name = name.Remove(name.IndexOf("\0"),name.Length-name.IndexOf("\0"));
                names.Add(Encoding.Unicode.GetString(catalog[j].Name));
            }
            return names;

        }

        public cat ReadFiles(string CompareName)
        {
            List<string> names = new List<string>();
            List<int> inodes = new List<int>();
            List<cat> catalog = ReadCatRecords(Inode_table[0].BlockPointer[0]);

            int i = 0;
            while (i < catalog.Count)
            {
                if (catalog[i].Inode_num == 0)
                {
                    catalog.RemoveAt(i);
                    i -= 1;
                }
                else i++;
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



        private void AddCatRecord(cat record)
        {
            bool b12 = false;
            bool b13 = false;
            bool b14 = false;
            for (var i = 0; i < 12; i++)
            {
                if (Inode.BlockPointer[i] != 0)
                {
                    List<cat> list = ReadCatRecords((int)Inode.BlockPointer[i]);
                    for (var j = 0; j < list.Count; j++)
                    {
                        if (list[j].Type == 0)
                        {
                            byte[] data = record.Get();
                            FileSystemIO.WriteFile(OpenPath, data, Constants.CAT_SIZE, ((int)Inode.BlockPointer[i] * Constants.BLOCK_SIZE) + (j * Constants.INODE_SIZE));
                            b12 = true;
                            return;
                        }
                    }
                }
                else
                {
                    var freeblock = Blocks_Bitmap.GetFreeBlock();
                    //пишем новый блок с элементами каталога
                    cat[] catRecords = new cat[Constants.BLOCK_SIZE / Constants.INODE_SIZE];
                    byte[] data = new byte[Constants.BLOCK_SIZE];
                    for (int ii = 0; ii < catRecords.Length; ii++)
                    {
                        catRecords[ii] = new cat();
                    }
                    foreach (cat c in catRecords)
                    {
                        var offset = 0;
                        byte[] block = c.Get();
                        Buffer.BlockCopy(block, 0, data, offset, block.Length);
                        offset += block.Length;
                    }
                    FileSystemIO.WriteFile(OpenPath, data, Constants.BLOCK_SIZE, freeblock * Constants.BLOCK_SIZE);//записали в фс записи
                    Inode.BlockPointer[i] = (uint)freeblock;
                    Blocks_Bitmap.SetBitmapState(freeblock, true);
                    InodeBitmap.SetBitmapState(InodeBitmap.GetFreeBlock(), true);//заняли рандомный блок, т.к. блоков меньше стало
                    Blocks_Bitmap.FreeBlocks -= 1;
                    InodeBitmap.FreeBlocks -= 1;
                    //---------------------------------------------------------
                    List<cat> list = ReadCatRecords((int)Inode.BlockPointer[i]);
                    for (var j = 0; j < list.Count; j++)
                    {
                        if (list[j].Type == 0)
                        {
                            data = record.Get();
                            FileSystemIO.WriteFile(OpenPath, data, Constants.CAT_SIZE, ((int)Inode.BlockPointer[i] * Constants.BLOCK_SIZE) + (j * Constants.INODE_SIZE));
                            //записаны сами данные в блок
                            b12 = true;
                            return;
                        }
                    }
                }
            }
            if (b12 == false)
                if (Inode.BlockPointer[12] != 0)
                {
                    //читаем блок с адресами блоков
                    List<int> Ilist = ReadAddressBlock((int)Inode.BlockPointer[12]);
                    for (var i =0;i<Ilist.Count;i++)
                    {
                        if (Ilist[i]==0)//адрес указывает в никуда поэтому создаем блок с записями
                        {
                            var freeblock = Blocks_Bitmap.GetFreeBlock();
                            //пишем новый блок с элементами каталога
                            cat[] catRecords = new cat[Constants.BLOCK_SIZE / Constants.INODE_SIZE];
                            byte[] data = new byte[Constants.BLOCK_SIZE];
                            foreach (cat c in catRecords)
                            {
                                var offset = 0;
                                byte[] block = c.Get();
                                Buffer.BlockCopy(block, 0, data, offset, block.Length);
                                offset += block.Length;
                            }
                            FileSystemIO.WriteFile(OpenPath, data, Constants.BLOCK_SIZE, freeblock * Constants.BLOCK_SIZE);//записали в фс записи
                            Ilist[i] = freeblock;//теперь адрес укзаывает на блок с элементами каталога
                            Blocks_Bitmap.SetBitmapState(freeblock, true);
                            InodeBitmap.SetBitmapState(InodeBitmap.GetFreeBlock(), true);//заняли рандомный блок, т.к. блоков меньше стало
                            Blocks_Bitmap.FreeBlocks -= 1;
                            InodeBitmap.FreeBlocks -= 1;
                            //---------------------------------------------------------
                            List<cat> list = ReadCatRecords((int)Inode.BlockPointer[i]);//читаем созданный блок с элементами каталога
                            for (var j = 0; j < list.Count; j++)
                            {
                                if (list[j].Type == 0)//ищем незаполненные
                                {
                                    data = record.Get();
                                    FileSystemIO.WriteFile(OpenPath, data, Constants.CAT_SIZE, ((int)Inode.BlockPointer[i] * Constants.BLOCK_SIZE) + (j * Constants.INODE_SIZE));
                                    //записаны сами данные в блок
                                    b13 = true;
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    int freeblock = Blocks_Bitmap.GetFreeBlock();//выделяем блок под блок с адресами блоков
                     //пишем данные в блок
                    int[] intdata = new int[Constants.BLOCK_SIZE / sizeof(int)];
                    byte[] data = new byte[Constants.BLOCK_SIZE];
                    foreach (int t in intdata)
                    {
                        int offset=0;
                        Buffer.BlockCopy(BitConverter.GetBytes(t), 0, data, offset, sizeof(int));
                        offset += sizeof(int);
                    }
                    FileSystemIO.WriteFile(OpenPath, data, Constants.BLOCK_SIZE, freeblock * Constants.BLOCK_SIZE);//записали в фс блок с адресами
                    Inode.BlockPointer[12] = (uint)freeblock;
                    //заполняем блок элементами каталога
                    
                    List<int> intlist = ReadAddressBlock((int)Inode.BlockPointer[12]);
                    for (var i = 0; i < intlist.Count;i++ )
                        if ((intlist.Count == 0)|| (intlist[i]==0))//если пустой адрес, с элементами каталога, то нужно его создать
                        {
                            int fblock = Blocks_Bitmap.GetFreeBlock();
                            //пишем новый блок с элементами каталога
                            cat[] catRecords = new cat[Constants.BLOCK_SIZE / Constants.INODE_SIZE];
                            byte[] data2 = new byte[Constants.BLOCK_SIZE];
                            foreach (cat c in catRecords)
                            {
                                var offset = 0;
                                byte[] block = c.Get();
                                Buffer.BlockCopy(block, 0, data2, offset, block.Length);
                                offset += block.Length;
                            }
                            FileSystemIO.WriteFile(OpenPath, data2, Constants.BLOCK_SIZE, fblock * Constants.BLOCK_SIZE);//записали в фс записи
                            intlist[i] = fblock;
                            Blocks_Bitmap.SetBitmapState(fblock, true);
                            InodeBitmap.SetBitmapState(InodeBitmap.GetFreeBlock(), true);
                            Blocks_Bitmap.FreeBlocks -= 1;
                            InodeBitmap.FreeBlocks -= 1;
                        }

                    for (var i=0;i<intlist.Count;i++)
                    {
                        if( intlist[i]!=0)
                        {
                            List<cat> list = ReadCatRecords((int)intlist[i]);//читаем созданный блок с элементами каталога
                            for (var j = 0; j < list.Count; j++)
                            {
                                if (list[j].Type == 0)//ищем незаполненные
                                {
                                    data = record.Get();
                                    FileSystemIO.WriteFile(OpenPath, data, Constants.CAT_SIZE, ((int)intlist[i] * Constants.BLOCK_SIZE) + (j * Constants.INODE_SIZE));
                                    //записаны сами данные в блок
                                    b13 = true;
                                    return;
                                }
                            }
                        }
                    
                    }
                    

                }
        }
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
            for (var i = 0; i < size;++i)
            {
                Buffer.BlockCopy(data, offset, part, 0, part.Length);
                offset += part.Length;
                FileSystemIO.WriteFile(Path, part, Constants.BLOCK_SIZE, list[i]*Constants.BLOCK_SIZE);
            }
            if (data.Length % Constants.BLOCK_SIZE != 0)
            {
                byte[] temp = new byte[data.Length % Constants.BLOCK_SIZE];
                Buffer.BlockCopy(data, offset, temp, 0, temp.Length);
                for (var i = 0; i < temp.Length; i++)
                    extend[i] = temp[i];
                FileSystemIO.WriteFile(Path, extend, Constants.BLOCK_SIZE, list[size] * Constants.BLOCK_SIZE);
            }

                
        }

        private List<int> ReadAddressBlock(int block)
        {
            List<int> result = new List<int>();
            int offset = 0;
            byte[] data = FileSystemIO.ReadFile(OpenPath, Constants.BLOCK_SIZE, block * Constants.BLOCK_SIZE, Constants.BLOCK_SIZE);
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


