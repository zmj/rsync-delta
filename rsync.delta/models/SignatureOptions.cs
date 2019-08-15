using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    public readonly struct SignatureOptions
    {
        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);

        public readonly int BlockLength;
        public readonly int StrongHashLength;
        
        public SignatureOptions(int blockLength, int strongHashLength)
        {
            if (blockLength <= 0) // todo: max blocklen
            {
                throw new ArgumentOutOfRangeException(nameof(BlockLength));
            }
            if (strongHashLength <= 0 || strongHashLength > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(StrongHashLength));
            }
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }
    }
}