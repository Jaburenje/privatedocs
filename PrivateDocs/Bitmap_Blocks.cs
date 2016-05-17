using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrivateDocs
{
    class Bitmap_Blocks
    {
        public byte[] indexMap { get; private set; } //1xN
        public int FreeBlocks; //4
        public int TotalBlocks; //4
        /// <summary>
        /// Конструктор битовой карты
        /// </summary>
        /// <param name="blocks_count"></param>
        public Bitmap_Blocks(int blocks_count)
        {
            Debug.Assert(blocks_count > 0);
            TotalBlocks = blocks_count;
            var size = TotalBlocks / Constants.BIT_IN_BYTE_COUNT;
            var lastBit = TotalBlocks % Constants.BIT_IN_BYTE_COUNT;
            size += ((lastBit) > 0 ? 1 : 0);

            try
            {
                indexMap = new byte[size];
                var byteIndex = size - 1;
                for (var i=Constants.BIT_IN_BYTE_COUNT-1;i<lastBit;i--)
                {
                    SetBit(ref indexMap[byteIndex], i, false);//true
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Bitmap construction is unsuccessful\n"+ ex);
                Debug.Assert(false);
            }
            FreeBlocks = TotalBlocks;
        }

        public Bitmap_Blocks(byte[] bitmap)
        {
            var offset = 0;
            byte[] TotalBlocks = new byte[Marshal.SizeOf(this.TotalBlocks)];
            Buffer.BlockCopy(bitmap, offset, TotalBlocks, 0, TotalBlocks.Length);
            this.TotalBlocks = BitConverter.ToInt32(TotalBlocks, 0);
            offset += TotalBlocks.Length;


            byte[] FreeBlocks = new byte[Marshal.SizeOf(this.FreeBlocks)];
            Buffer.BlockCopy(bitmap, offset, FreeBlocks, 0, FreeBlocks.Length);
            this.FreeBlocks = BitConverter.ToInt32(FreeBlocks, 0);
            offset += FreeBlocks.Length;

            indexMap = new byte[bitmap.Length - offset];
            Buffer.BlockCopy(bitmap, offset, indexMap, 0, indexMap.Length);
            offset += indexMap.Length;
        }
        /// <summary>
        /// Установка битов в байте
        /// </summary>
        /// <param name="b">байт в который передаем значение</param>
        /// <param name="bitNumber">номер бита в котором меняем</param>
        /// <param name="value">значение</param>
        private void SetBit(ref byte b, int bitNumber, bool value)
        {
            b = value ? (byte)(b | (1 << bitNumber)) : (byte)(b & ~(1 << bitNumber));
        }

        /// <summary>
        /// Возвращает установленное значение в бите байта. true = 1; false = 0
        /// </summary>
        /// <param name="b">  Байт </param>
        /// <param name="bitNumber"> Номер бита в байте </param>
        /// <returns> Значение в бите байта </returns>
        private bool GetBit(byte b, int bitNumber)
        {
            bool result = (b & (1 << bitNumber)) != 0;
            return result;
        }

        public int GetSize()
        {
            var result = (indexMap.Length + Marshal.SizeOf(FreeBlocks) + Marshal.SizeOf(TotalBlocks));
            return result;
        }



        public byte[] Get()
        {
            var size = GetSize();
            byte[] result = new byte[size];
            int offset = 0;
            
            byte[] TotalBlocks = BitConverter.GetBytes(this.TotalBlocks);
            Buffer.BlockCopy(TotalBlocks, 0, result, offset, TotalBlocks.Length);
            offset += TotalBlocks.Length;

            byte[] FreeBlocks = BitConverter.GetBytes(this.FreeBlocks);
            Buffer.BlockCopy(FreeBlocks, 0, result, offset, FreeBlocks.Length);
            offset += FreeBlocks.Length;

            Buffer.BlockCopy(indexMap, 0, result, offset, indexMap.Length);
            offset += indexMap.Length;
            return result;
        }
        /// <summary>
        /// Подсчет количества свободных блоков
        /// </summary>
        /// <returns></returns>
        public int CountFreeBlocks()
        {
            var locker = new object();
            var result=0;
            Parallel.ForEach(Partitioner.Create(0, indexMap.Length), (range, state) =>
                {
                    var tmp = 0;
                    for (var i = range.Item1; i < range.Item2; i++)
                        for (var j = 0; j < Constants.BIT_IN_BYTE_COUNT; j++)
                        {
                            tmp += GetBit(indexMap[i], j) ? 0 : 1;
                        }
                    lock (locker)
                    {
                        result += tmp;
                    }                    
                });
            return result;
        }

        public int CountTotalBlocks()
        {
            var locker = new object();
            var result = 0;
            Parallel.ForEach(Partitioner.Create(0, indexMap.Length), (range, state) =>
            {
                var tmp = 0;
                for (var i = range.Item1; i < range.Item2; i++)
                    for (var j = 0; j < Constants.BIT_IN_BYTE_COUNT; j++)
                    {
                        tmp++;
                    }
                lock (locker)
                {
                    result += tmp;
                }
            });
            return result;
        }
        /// <summary>
        /// Вытягиваем индексы свободных блоков
        /// </summary>
        /// <returns></returns>
        public List<int> FreeBlocksIndex()
        {
            List<int> freeBlocks = new List<int>();
            for (var i=0;i<indexMap.Length;i++)
            {
                for (var j=0;j<Constants.BIT_IN_BYTE_COUNT;j++)
                {
                    if (!GetBit(indexMap[i],j))
                    {
                        freeBlocks.Add(Constants.BIT_IN_BYTE_COUNT * i + j);               
                    }
                }
            }
            return freeBlocks;
        }
        /// <summary>
        /// вытягиваем индексы свободных блоков в нужном количестве
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<int> FreeBlocksIndex(int count)
        {
            List<int> result = new List<int>();
            int tmp = 0;
            for (var i = 0; i < indexMap.Length; i++)
            {
                for (var j = 0; j < Constants.BIT_IN_BYTE_COUNT; j++)
                {
                    if (!GetBit(indexMap[i], j))
                    {
                        result.Add(Constants.BIT_IN_BYTE_COUNT * i + j);
                        tmp++;
                        if (tmp >= count)
                            return result;
                    }
                }
            }
            return null;
        }

        public int GetFreeBlock()
        {
            for (var i = 0; i < indexMap.Length; i++)
            {
                for (var j = 0; j < Constants.BIT_IN_BYTE_COUNT; j++)
                {
                    if (!GetBit(indexMap[i], j))
                    {
                        return Constants.BIT_IN_BYTE_COUNT * i + j;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Установка значения блоку в битовой карте
        /// </summary>
        /// <param name="index">Индекс блока</param>
        /// <param name="value">Значение (false свободный, true занятый)</param>
        public void SetBitmapState(int index, bool value)
        {
            int ByteIndex = 0;
            int BitInByteIndex = 0;
            ByteIndex = index / Constants.BIT_IN_BYTE_COUNT;
            BitInByteIndex = index % Constants.BIT_IN_BYTE_COUNT;
            SetBit(ref indexMap[ByteIndex], BitInByteIndex, value);

            FreeBlocks += (value == true) ? -1 : 1;
        }

        public void SetBitmapState(List<int> list,bool value)
        {
            int ByteIndex = 0;
            int BitInByteIndex = 0;
            Parallel.ForEach(Partitioner.Create(0, list.Count), (range, state) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        ByteIndex = list[i] / Constants.BIT_IN_BYTE_COUNT;
                        BitInByteIndex = list[i] % Constants.BIT_IN_BYTE_COUNT;
                        SetBit(ref indexMap[ByteIndex], BitInByteIndex, value);
                    }
                });
            FreeBlocks += (value == true) ? -list.Count : list.Count;
        }


    }
}
