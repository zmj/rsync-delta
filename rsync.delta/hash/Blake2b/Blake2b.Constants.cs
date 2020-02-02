#if !NETSTANDARD2_0
using System.Runtime.Intrinsics.X86;
#endif

namespace Rsync.Delta.Hash.Blake2b
{
    internal static class Constants
    {
        public const int Rounds = 12;

        public static readonly ulong[] IV = new ulong[8]
        {
            0x6A09E667F3BCC908UL,
            0xBB67AE8584CAA73BUL,
            0x3C6EF372FE94F82BUL,
            0xA54FF53A5F1D36F1UL,
            0x510E527FADE682D1UL,
            0x9B05688C2B3E6C1FUL,
            0x1F83D9ABFB41BD6BUL,
            0x5BE0CD19137E2179UL,
        };

        public static readonly int[] MessagePermutation = new int[Rounds * 16]
        {
            // round 1
            0,  2,  4,  6,
            1,  3,  5,  7,
            8,  10, 12, 14,
            9,  11, 13, 15,
            // round 2
            14, 4,  9,  13,
            10, 8,  15, 6,
            1,  0,  11, 5,
            12, 2,  7,  3,
            // round 3
            11, 12, 5,  15,
            8,  0,  2,  13,
            10, 3,  7,  9,
            14, 6,  1,  4,
            // round 4
            7,  3,  13, 11,
            9,  1,  12, 14,
            2,  5,  4,  15,
            6,  10, 0,  8,
            // round 5
            9,  5,  2,  10,
            0,  7,  4,  15,
            14, 11, 6,  3,
            1,  12, 8,  13,
            // round 6
            2,  6,  0,  8,
            12, 10, 11, 3,
            4,  7,  15, 1,
            13, 5,  14, 9,
            // round 7
            12, 1,  14, 4,
            5,  15, 13, 10,
            0,  6,  9,  8,
            7,  3,  2,  11,
            // round 8
            13, 7,  12, 3,
            11, 14, 1,  9,
            5,  15, 8,  2,
            0,  4,  6,  10,
            // round 9
            6,  14, 11, 0,
            15, 9,  3,  8,
            12, 13, 1,  10,
            2,  7,  4,  5,
            // round 10
            10, 8,  7,  1,
            2,  4,  6,  5,
            15, 9,  3,  13,
            11, 14, 12, 0,                
            // round 11 (1)
            0,  2,  4,  6,
            1,  3,  5,  7,
            8,  10, 12, 14,
            9,  11, 13, 15,
            // round 12 (2)
            14, 4,  9,  13,
            10, 8,  15, 6,
            1,  0,  11, 5,
            12, 2,  7,  3,
        };

#if NETSTANDARD2_0
        public const int ScratchLength =
            Blake2bCore.BlockLength +
            Blake2bCore.MaxHashLength +
            Blake2bScalar.ScratchLength;
#else
        public static readonly int ScratchLength =
            Blake2bCore.BlockLength +
            Blake2bCore.MaxHashLength +
            (Avx2.IsSupported ?
                Blake2bAvx2.ScratchLength :
                Blake2bScalar.ScratchLength);

        public static readonly byte[] ShuffleMask16 = new byte[]
        {
            2,  3,  4,  5,  6,  7,  0, 1,
            10, 11, 12, 13, 14, 15, 8, 9,
        };
#endif
    }
}
