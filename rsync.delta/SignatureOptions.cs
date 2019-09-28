using System;

namespace Rsync.Delta
{
    public readonly struct SignatureOptions
    {
        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);

        public readonly int BlockLength;
        public readonly int StrongHashLength;

        public SignatureOptions(int blockLength, int strongHashLength)
        {
            if (blockLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(BlockLength));
            }
            if (strongHashLength <= 0 || strongHashLength > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(StrongHashLength));
            }
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }
    }
}
