using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    class Controller
    {
       public SB SuperBlock;
       public Bitmap_Blocks InodeBitmap;
       public Bitmap_Blocks Blocks_Bitmap;
       public inode_struct Inode;
    public Controller()
    {
    SuperBlock=new SB();
    InodeBitmap = null;
    Blocks_Bitmap = null;
    Inode = null;
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
        public void ReadServiceInfo(byte[] SBlock,string Path)
        {
            SB ReadedSB = new SB(SBlock);
            SuperBlock = ReadedSB;
            byte[] BBlock = FileSystemIO.ReadFile(Path,Constants.BLOCK_SIZE,Constants.BLOCK_SIZE,(SuperBlock.sb_block_map_index-1)*Constants.BLOCK_SIZE);
            Bitmap_Blocks BB = new Bitmap_Blocks(BBlock);
            Blocks_Bitmap = BB;
            byte[] IBlock = FileSystemIO.ReadFile(Path, Constants.BLOCK_SIZE, Constants.BLOCK_SIZE*SuperBlock.sb_block_map_index, (SuperBlock.sb_inodes_map_index-SuperBlock.sb_block_map_index) * Constants.BLOCK_SIZE);
            Bitmap_Blocks IB = new Bitmap_Blocks(IBlock);
            InodeBitmap = IB;
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
            SuperBlock.sb_block_size = Constants.BLOCK_SIZE;
            SuperBlock.sb_first_data_block = Blocks_Bitmap.TotalBlocks - Blocks_Bitmap.FreeBlocks+1;
            SuperBlock.sb_errors = 0;
            SuperBlock.sb_state = 'c';
            SuperBlock.sb_last_check = -1;
            SuperBlock.sb_check_time = Constants.CHECK_INTERVAL;
            SuperBlock.sb_mount_max = Constants.MAX_MOUNT_COUNT;       
            #endregion
            Inode = CreateRootDirectory(Inodes[0]);
            byte[] byteroot = new byte[Constants.BLOCK_SIZE];

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
            byte[] size = BitConverter.GetBytes(SuperBlock.GetSize());
            //Buffer.BlockCopy(size, 0, result, offset, size.Length);
            //offset += size.Length;

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
                Buffer.BlockCopy(inode, 0, In_arr, offset, inode.Length);
                offset += inode.Length;
            }
            var block_count = In_arr.Length / Constants.BLOCK_SIZE + (In_arr.Length % Constants.BLOCK_SIZE > 0 ? 1 : 0);
            byte[] result = new byte[block_count * Constants.BLOCK_SIZE];
            Buffer.BlockCopy(In_arr, 0, result, 0, In_arr.Length);

            return result;
        }
        
        public inode_struct CreateRootDirectory(inode_struct root)
        {
            root.Set('d', 0, new uint[Constants.INODE_BLOCKS]);
            uint freeblock = (uint)Blocks_Bitmap.GetFreeBlock();// заняли блок
            Debug.Assert(!freeblock.Equals(-1));

            root.BlockPointer[0] = freeblock;
            Blocks_Bitmap.SetBitmapState((int)root.BlockPointer[0], true);
            SuperBlock.sb_free_blocks_count -= 1;
            InodeBitmap.SetBitmapState((int)root.BlockPointer[0],true);
            SuperBlock.sb_free_inodes_count -= 1;
            return root;
        }

    }
}
