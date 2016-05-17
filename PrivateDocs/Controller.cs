using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
                byte[] tmp = new byte[Marshal.SizeOf(Inode_table[0])];
                Buffer.BlockCopy(ITable, offset, tmp, 0, tmp.Length);
                offset += tmp.Length;
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
            byte[] byteroot = new byte[Constants.BLOCK_SIZE];
            foreach (cat c in catRecords)
            {
                var offset = 0;
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
            for (var i=0;i<Marshal.SizeOf(data)/Constants.BLOCK_SIZE;i++)
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

        public cat AddFile(byte[] name,string Path,string PathtoFile)
        {
            byte[] data = FileSystemIO.ReadFile(PathtoFile,Constants.BLOCK_SIZE);//читаем полностью файл который будем писать
            var size=Marshal.SizeOf(data);
            var SizeinBlocks = (size / Constants.BLOCK_SIZE + (size % Constants.BLOCK_SIZE > 0 ? 1 : 0));
            Debug.Assert(SuperBlock.sb_free_blocks_count>SizeinBlocks);
            Debug.Assert(SuperBlock.sb_free_inodes_count>SizeinBlocks);
            var count = 0;
            if (SizeinBlocks > 12) 
                count++;
            if (SizeinBlocks>1024)
            {
                count++;
                count += ((SizeinBlocks - 1024) / 1000 + (SizeinBlocks - 1024) % 1000 > 0 ? 1 : 0);
            }
            List<int> blocks = new List<int>();
            List<int> Iblocks = new List<int>();
            if ((Blocks_Bitmap.FreeBlocks>=SizeinBlocks+count)&&(InodeBitmap.FreeBlocks>0))
            {
                var freeblock = InodeBitmap.GetFreeBlock();
                blocks = Blocks_Bitmap.FreeBlocksIndex(SizeinBlocks + count);
                

                
                WriteFileToFS(Path, blocks, data);//пишем сам файл
                for (var i = 0; i < blocks.Count;i++ )
                {
                   if (i<12)
                   {
                       Inode_table[freeblock].BlockPointer[i] = (uint)blocks[i];
                   }
                    if ((i>=12)&&(i<1024))
                    {
                        var newblock = Blocks_Bitmap.GetFreeBlock();

                        Inode_table[freeblock].BlockPointer[12] = (uint)newblock;//выделили блок под адреса
                        byte[] adrblock = new byte[Constants.BLOCK_SIZE];
                       for (var j=12; j<blocks.Count;j++)
                           if (j<1036)
                           Buffer.BlockCopy(BitConverter.GetBytes(blocks[j]), 0, adrblock, sizeof(int), sizeof(int));//пишем адреса
                       
                       FileSystemIO.WriteFile(OpenPath, adrblock, Constants.BLOCK_SIZE, newblock * Constants.BLOCK_SIZE);//пишем уже в фс
                       Blocks_Bitmap.FreeBlocks -= 1;
                    }
                }
                Blocks_Bitmap.SetBitmapState(blocks, true);//блокируем биты в битовой карте
                InodeBitmap.SetBitmapState(freeblock, true);//блокируем биты в Inode битовой карте
                Blocks_Bitmap.FreeBlocks-=blocks.Count;
                InodeBitmap.FreeBlocks-=1;
                cat Record = new cat();
                Record.Name = name;
                Record.Inode_num = freeblock;
                Record.Size = size;
                Record.Type = 20;

                AddCatRecord(Record);
                

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
            var offset = 0;
            byte[] part = new byte[Constants.BLOCK_SIZE];
            for (var i = 0; i < list.Count - 1;++i)
            {
                Buffer.BlockCopy(data, offset, part, 0, part.Length);
                offset += part.Length;
                FileSystemIO.WriteFile(Path, part, Constants.BLOCK_SIZE, list[i]*Constants.BLOCK_SIZE);

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
    }

    public class AddressBlock
    {
        int[] Address;

        public AddressBlock()
        {
            Address=new int[Constants.BLOCK_SIZE/sizeof(int)];//получается 1024 адреса на блоки по 4кб=4096б
        }
    }

    public class DoubleAB
    {
        AddressBlock[] Block;
        public DoubleAB()
        {
            Block = new AddressBlock[Constants.BLOCK_SIZE / sizeof(int)];//а здесь получается 1024 адреса на блоки по 1024 адреса=4194304кб=4096мб
        }
    }
}


