using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rsync.Delta.Hash.Blake2b
{
    internal static class Blake2bScalar
    {
        public const int ScratchLength = 128;

        public static void HashBlock(
            ReadOnlySpan<ulong> block,
            Span<ulong> scratch,
            Span<ulong> hash,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag)
        {
            Debug.Assert(block.Length == 16);
            Debug.Assert(scratch.Length == 16);
            Debug.Assert(hash.Length == 8);

            hash.CopyTo(scratch);
            Constants.IV.CopyTo(scratch.Slice(8));
            scratch[12] ^= bytesHashed;
            scratch[13] ^= bytesHashedOverflows;
            scratch[14] ^= finalizationFlag;
            Rounds(scratch, block);
            Compress(hash, scratch);
        }

        private static void Rounds(
            Span<ulong> scratch,
            ReadOnlySpan<ulong> msg)
        {
            for (int r = 0; r < Constants.Rounds; r++)
            {
                G(scratch, msg, 0, 4, 8, 12, r, 0);
                G(scratch, msg, 1, 5, 9, 13, r, 1);
                G(scratch, msg, 2, 6, 10, 14, r, 2);
                G(scratch, msg, 3, 7, 11, 15, r, 3);

                G(scratch, msg, 0, 5, 10, 15, r, 8);
                G(scratch, msg, 1, 6, 11, 12, r, 9);
                G(scratch, msg, 2, 7, 8, 13, r, 10);
                G(scratch, msg, 3, 4, 9, 14, r, 11);
            }
        }

        private static void Compress(
            Span<ulong> hash,
            ReadOnlySpan<ulong> scratch)
        {
            for (int i = 0; i < 8; ++i)
            {
                hash[i] ^= scratch[i] ^ scratch[i + 8];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(
            Span<ulong> scratch,
            ReadOnlySpan<ulong> msg,
            int a, int b, int c, int d, int r, int i)
        {
            int p = (r << 4) + i;
            scratch[a] += scratch[b] + msg[Constants.MessagePermutation[p]];
            scratch[d] = RotateRight(scratch[d] ^ scratch[a], 32);
            scratch[c] += scratch[d];
            scratch[b] = RotateRight(scratch[b] ^ scratch[c], 24);
            scratch[a] += scratch[b] + msg[Constants.MessagePermutation[p + 4]];
            scratch[d] = RotateRight(scratch[d] ^ scratch[a], 16);
            scratch[c] += scratch[d];
            scratch[b] = RotateRight(scratch[b] ^ scratch[c], 63);

            static ulong RotateRight(ulong value, int nBits) =>
                (value >> nBits) | (value << (64 - nBits));
        }
    }
}
