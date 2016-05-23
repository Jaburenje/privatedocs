using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
namespace PrivateDocs
{
    public static class Constants
    {
        public static readonly UInt16 MAX_NAME_LENGTH = 118;														 // 2
        public static readonly UInt16 CAT_SIZE = 128;														         // 2
        public static readonly UInt16 INODE_SIZE = 128;														         // 2
        public static readonly UInt16 INODE_BLOCKS = 15;                                                             // 2
        public static readonly UInt16 BIT_IN_BYTE_COUNT = 8;														 // 2
        public static readonly UInt16 BLOCK_SIZE = 4096; 													         // 2
        public static readonly UInt16 PASS_OFFSET = 76;                                                              // 2
        public static readonly UInt16 MAX_PASSWORD_LENGTH = 16;                                                      // 2
        public static readonly UInt16 MAX_MOUNT_COUNT = 1024;                                                        // 2
        public static readonly Int64 CHECK_INTERVAL = 26548862600153; // about 30 days in ticks						 // 8
        public static readonly Int32 MAX_BLOCK_COUNT = Int32.MinValue;                                               // 4       
        }
    }
    
