using System;
using System.Diagnostics;

namespace Rsync.Delta.Hash.Blake2b
{
    internal static class Blake2bScalar
    {
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

        private static void Rounds(Span<ulong> v, ReadOnlySpan<ulong> m)
        {
            for (int r = 0; r < Constants.Rounds; ++r)
            {
                G(v, m, 0, 4, 8, 12, r, 0);
                G(v, m, 1, 5, 9, 13, r, 1);
                G(v, m, 2, 6, 10, 14, r, 2);
                G(v, m, 3, 7, 11, 15, r, 3);
                G(v, m, 3, 4, 9, 14, r, 11);
                G(v, m, 2, 7, 8, 13, r, 10);
                G(v, m, 0, 5, 10, 15, r, 8);
                G(v, m, 1, 6, 11, 12, r, 9);
            }
        }

        private static void Compress(Span<ulong> h, ReadOnlySpan<ulong> v)
        {
            for (int i = 0; i < 8; ++i)
            {
                h[i] ^= v[i] ^ v[i + 8];
            }
        }

        private static void G(
            Span<ulong> v,
            ReadOnlySpan<ulong> m,
            int a, int b, int c, int d, int r, int i)
        {
            int p = (r << 4) + i;
            v[a] += v[b] + m[Constants.MessagePermutation[p]];
            v[d] = RotateRight(v[d] ^ v[a], 32);
            v[c] += v[d];
            v[b] = RotateRight(v[b] ^ v[c], 24);
            v[a] += v[b] + m[Constants.MessagePermutation[p + 4]];
            v[d] = RotateRight(v[d] ^ v[a], 16);
            v[c] += v[d];
            v[b] = RotateRight(v[b] ^ v[c], 63);

            static ulong RotateRight(ulong value, int nBits) =>
                (value >> nBits) | (value << (64 - nBits));
        }
    }
}
