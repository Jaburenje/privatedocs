using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace PrivateDocs
{
    class SB
    {
        public int sb_blocks_count { get; set; }// общее число блоков в файловой системе; 4
        public int sb_free_blocks_count { get; set; }// количество свободных блоков; 4
        public int sb_block_map_index { get; set; }//номер блока фс с битовой картой блоков 4
        public int sb_inodes_count { get; set; }// общее число inode в файловой системе(индексных дескрипторов); 4
        public int sb_free_inodes_count { get; set; }// количество свободных inode; 4
        public int sb_inodes_map_index { get; set; }// номер последнего блока фс с битовой картой inodes 4
        public int sb_first_data_block { get; set; }// номер первого блока данных; 4
        public ushort sb_state { get; set; }// состояние фс 2
        public ushort sb_errors { get; set; }// ошибки 2
        public ushort sb_mount_count { get; set; }/* Счетчик числа монтирований фс */// 2
        public ushort sb_mount_max { get; set; } /* Максимальное число монтирований фс */// 2
        public int sb_block_size { get; set; } /* Размер блока */ //4
        public long sb_mount_time { get; set; } /* Время последнего монтирования фс */ //8 
        public long sb_write_time { get; set; } /* Время последней записи в фс */ //8
        public long sb_check_time { get; set; }//период времени между проверками //8
        public long sb_last_check { get; set; }//время последней проверки фс //8
        public byte[] Password { get; set; } /* Пароль */ //16
        //88
      
        /// <summary>
        /// Инициализация суперблока
        /// </summary>
        public SB()
        {
            //Password = new byte[Constants.MAX_PASSWORD_LENGTH];
        }
        /// <summary>
        /// заполнение суперблока
        /// </summary>
        public SB(
            int sb_blocks_count,
            int sb_free_blocks_count,
            int sb_block_map_index,
            int sb_inodes_count,
            int sb_free_inodes_count,
            int sb_inodes_map_index,
            int sb_first_data_block,
            ushort sb_state,
            ushort sb_errors,
            ushort sb_mount_count,
            ushort sb_mount_max,
            int sb_block_size,
            long sb_mount_time,
            long sb_write_time,
            long sb_check_time,
            long sb_last_check,
            byte[] Password
            )
        {
            this.sb_blocks_count = sb_blocks_count;
            this.sb_free_blocks_count = sb_free_blocks_count;
            this.sb_block_map_index = sb_block_map_index;
            this.sb_inodes_count = sb_inodes_count;
            this.sb_free_inodes_count = sb_free_inodes_count;
            this.sb_inodes_map_index = sb_inodes_map_index;
            this.sb_first_data_block = sb_first_data_block;
            this.sb_state = sb_state;
            this.sb_errors = sb_errors;
            this.sb_mount_count = sb_mount_count;
            this.sb_mount_max = sb_mount_max;
            this.sb_block_size = sb_block_size;
            this.sb_mount_time = sb_mount_time;
            this.sb_write_time = sb_write_time;
            this.sb_check_time = sb_check_time;
            this.sb_last_check = sb_last_check;
            this.Password = new byte[Constants.MAX_PASSWORD_LENGTH];
            Buffer.BlockCopy(Password, 0, this.Password, 0, Password.Length);
        }

        public SB(byte[] ByteObj)
        {
            var offset = 0;
            byte[] sb_blocks_count = new byte[Marshal.SizeOf(this.sb_blocks_count)];
            Buffer.BlockCopy(ByteObj, offset, sb_blocks_count, 0, sb_blocks_count.Length);
            this.sb_blocks_count = BitConverter.ToInt32(sb_blocks_count, 0);
            offset += sb_blocks_count.Length;

            byte[] sb_free_blocks_count = new byte[Marshal.SizeOf(this.sb_free_blocks_count)];
            Buffer.BlockCopy(ByteObj, offset, sb_free_blocks_count, 0, sb_free_blocks_count.Length);
            this.sb_free_blocks_count = BitConverter.ToInt32(sb_free_blocks_count, 0);
            offset += sb_free_blocks_count.Length;

            byte[] sb_block_map_index = new byte[Marshal.SizeOf(this.sb_block_map_index)];
            Buffer.BlockCopy(ByteObj, offset, sb_block_map_index, 0, sb_block_map_index.Length);
            this.sb_block_map_index = BitConverter.ToInt32(sb_block_map_index, 0);
            offset += sb_block_map_index.Length;

            byte[] sb_inodes_count = new byte[Marshal.SizeOf(this.sb_inodes_count)];
            Buffer.BlockCopy(ByteObj, offset, sb_inodes_count, 0, sb_inodes_count.Length);
            this.sb_inodes_count = BitConverter.ToInt32(sb_inodes_count, 0);
            offset += sb_inodes_count.Length;

            byte[] sb_free_inodes_count = new byte[Marshal.SizeOf(this.sb_free_inodes_count)];
            Buffer.BlockCopy(ByteObj, offset, sb_free_inodes_count, 0, sb_free_inodes_count.Length);
            this.sb_free_inodes_count = BitConverter.ToInt32(sb_free_inodes_count, 0);
            offset += sb_free_inodes_count.Length;

            byte[] sb_inodes_map_index = new byte[Marshal.SizeOf(this.sb_inodes_map_index)];
            Buffer.BlockCopy(ByteObj, offset, sb_inodes_map_index, 0, sb_inodes_map_index.Length);
            this.sb_inodes_map_index = BitConverter.ToInt32(sb_inodes_map_index, 0);
            offset += sb_inodes_map_index.Length;

            byte[] sb_first_data_block = new byte[Marshal.SizeOf(this.sb_first_data_block)];
            Buffer.BlockCopy(ByteObj, offset, sb_first_data_block, 0, sb_first_data_block.Length);
            this.sb_first_data_block = BitConverter.ToInt32(sb_first_data_block, 0);
            offset += sb_first_data_block.Length;

            byte[] sb_state = new byte[Marshal.SizeOf(this.sb_state)];
            Buffer.BlockCopy(ByteObj, offset, sb_state, 0, sb_state.Length);
            this.sb_state = BitConverter.ToUInt16(sb_state, 0);
            offset += sb_state.Length;

            byte[] sb_errors = new byte[Marshal.SizeOf(this.sb_errors)];
            Buffer.BlockCopy(ByteObj, offset, sb_errors, 0, sb_errors.Length);
            this.sb_errors = BitConverter.ToUInt16(sb_errors, 0);
            offset += sb_errors.Length;

            byte[] sb_mount_count = new byte[Marshal.SizeOf(this.sb_mount_count)];
            Buffer.BlockCopy(ByteObj, offset, sb_mount_count, 0, sb_mount_count.Length);
            this.sb_mount_count = BitConverter.ToUInt16(sb_mount_count, 0);
            offset += sb_mount_count.Length;

            byte[] sb_mount_max = new byte[Marshal.SizeOf(this.sb_mount_max)];
            Buffer.BlockCopy(ByteObj, offset, sb_mount_max, 0, sb_mount_max.Length);
            this.sb_mount_max = BitConverter.ToUInt16(sb_mount_max, 0);
            offset += sb_mount_max.Length;

            byte[] sb_block_size = new byte[Marshal.SizeOf(this.sb_block_size)];
            Buffer.BlockCopy(ByteObj, offset, sb_block_size, 0, sb_block_size.Length);
            this.sb_block_size = BitConverter.ToInt32(sb_block_size, 0);
            offset += sb_block_size.Length;

            byte[] sb_mount_time = new byte[Marshal.SizeOf(this.sb_mount_time)];
            Buffer.BlockCopy(ByteObj, offset, sb_mount_time, 0, sb_mount_time.Length);
            this.sb_mount_time = BitConverter.ToInt64(sb_mount_time, 0);
            offset += sb_mount_time.Length;

            byte[] sb_write_time = new byte[Marshal.SizeOf(this.sb_write_time)];
            Buffer.BlockCopy(ByteObj, offset, sb_write_time, 0, sb_write_time.Length);
            this.sb_write_time = BitConverter.ToInt64(sb_write_time, 0);
            offset += sb_write_time.Length;

            byte[] sb_check_time = new byte[Marshal.SizeOf(this.sb_check_time)];
            Buffer.BlockCopy(ByteObj, offset, sb_check_time, 0, sb_check_time.Length);
            this.sb_check_time = BitConverter.ToInt64(sb_check_time, 0);
            offset += sb_check_time.Length;

            byte[] sb_last_check = new byte[Marshal.SizeOf(this.sb_last_check)];
            Buffer.BlockCopy(ByteObj, offset, sb_last_check, 0, sb_last_check.Length);
            this.sb_last_check = BitConverter.ToInt64(sb_last_check, 0);
            offset += sb_last_check.Length;

            this.Password = new byte[Constants.MAX_PASSWORD_LENGTH];
            Buffer.BlockCopy(ByteObj, offset, this.Password, 0, this.Password.Length);
            offset += this.Password.Length;
        }
        public int GetSize()
        {
            var result = Marshal.SizeOf(sb_blocks_count);
            result += Marshal.SizeOf(sb_free_blocks_count);
            result += Marshal.SizeOf(sb_block_map_index);
            result += Marshal.SizeOf(sb_inodes_count);
            result += Marshal.SizeOf(sb_free_inodes_count);
            result += Marshal.SizeOf(sb_inodes_map_index);
            result += Marshal.SizeOf(sb_first_data_block);
            result += Marshal.SizeOf(sb_state);
            result += Marshal.SizeOf(sb_errors);
            result += Marshal.SizeOf(sb_mount_count);
            result += Marshal.SizeOf(sb_mount_max);
            result += Marshal.SizeOf(sb_block_size);
            result += Marshal.SizeOf(sb_mount_time);
            result += Marshal.SizeOf(sb_write_time);
            result += Marshal.SizeOf(sb_check_time);
            result += Marshal.SizeOf(sb_last_check);
            result += Password.Length;
            return result;
        }
        public byte[] Get()
        {
            var size = GetSize();
            byte[] result = new byte[size];
            var offset = 0;
            byte[] sb_blocks_count = BitConverter.GetBytes(this.sb_blocks_count);
            Buffer.BlockCopy(sb_blocks_count, 0, result, offset, sb_blocks_count.Length);
            offset += sb_blocks_count.Length;

            byte[] sb_free_blocks_count = BitConverter.GetBytes(this.sb_free_blocks_count);
            Buffer.BlockCopy(sb_free_blocks_count, 0, result, offset, sb_free_blocks_count.Length);
            offset += sb_free_blocks_count.Length;

            byte[] sb_block_map_index = BitConverter.GetBytes(this.sb_block_map_index);
            Buffer.BlockCopy(sb_block_map_index, 0, result, offset, sb_block_map_index.Length);
            offset += sb_block_map_index.Length;

            byte[] sb_inodes_count = BitConverter.GetBytes(this.sb_inodes_count);
            Buffer.BlockCopy(sb_inodes_count, 0, result, offset, sb_inodes_count.Length);
            offset += sb_inodes_count.Length;

            byte[] sb_free_inodes_count = BitConverter.GetBytes(this.sb_free_inodes_count);
            Buffer.BlockCopy(sb_free_inodes_count, 0, result, offset, sb_free_inodes_count.Length);
            offset += sb_free_inodes_count.Length;

            byte[] sb_inodes_map_index = BitConverter.GetBytes(this.sb_inodes_map_index);
            Buffer.BlockCopy(sb_inodes_map_index, 0, result, offset, sb_inodes_map_index.Length);
            offset += sb_inodes_map_index.Length;

            byte[] sb_first_data_block = BitConverter.GetBytes(this.sb_first_data_block);
            Buffer.BlockCopy(sb_first_data_block, 0, result, offset, sb_first_data_block.Length);
            offset += sb_first_data_block.Length;

            byte[] sb_state = BitConverter.GetBytes(this.sb_state);
            Buffer.BlockCopy(sb_state, 0, result, offset, sb_state.Length);
            offset += sb_state.Length;

            byte[] sb_errors = BitConverter.GetBytes(this.sb_errors);
            Buffer.BlockCopy(sb_errors, 0, result, offset, sb_errors.Length);
            offset += sb_errors.Length;

            byte[] sb_mount_count = BitConverter.GetBytes(this.sb_mount_count);
            Buffer.BlockCopy(sb_mount_count, 0, result, offset, sb_mount_count.Length);
            offset += sb_mount_count.Length;

            byte[] sb_mount_max = BitConverter.GetBytes(this.sb_mount_max);
            Buffer.BlockCopy(sb_mount_max, 0, result, offset, sb_mount_max.Length);
            offset += sb_mount_max.Length;

            byte[] sb_block_size = BitConverter.GetBytes(this.sb_block_size);
            Buffer.BlockCopy(sb_block_size, 0, result, offset, sb_block_size.Length);
            offset += sb_block_size.Length;

            byte[] sb_mount_time = BitConverter.GetBytes(this.sb_mount_time);
            Buffer.BlockCopy(sb_mount_time, 0, result, offset, sb_mount_time.Length);
            offset += sb_mount_time.Length;

            byte[] sb_write_time = BitConverter.GetBytes(this.sb_write_time);
            Buffer.BlockCopy(sb_write_time, 0, result, offset, sb_write_time.Length);
            offset += sb_write_time.Length;

            byte[] sb_check_time = BitConverter.GetBytes(this.sb_check_time);
            Buffer.BlockCopy(sb_check_time, 0, result, offset, sb_check_time.Length);
            offset += sb_check_time.Length;

            byte[] sb_last_check = BitConverter.GetBytes(this.sb_last_check);
            Buffer.BlockCopy(sb_last_check, 0, result, offset, sb_last_check.Length);
            offset += sb_last_check.Length;
			               
       
            Buffer.BlockCopy(this.Password, 0,result, offset, this.Password.Length);
            offset += this.Password.Length;
            return result;
        }
    }

}
