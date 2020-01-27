using System;

namespace Rsync.Delta.Hash.Blake2b
{
    internal ref partial struct Blake2bCore
    {
        private static void HashBlockScalar(
            ReadOnlySpan<ulong> block,
            Span<ulong> scratch,
            Span<ulong> hash)
        {
            RoundsScalar(scratch, block);
            CompressScalar(hash, scratch);
        }

        private static void RoundsScalar(Span<ulong> v, ReadOnlySpan<ulong> m)
        {
            for (int r = 0; r < _numRounds; ++r)
            {
                G(v, m, 0, 4, 8, 12, r, 0);
                G(v, m, 1, 5, 9, 13, r, 2);
                G(v, m, 2, 6, 10, 14, r, 4);
                G(v, m, 3, 7, 11, 15, r, 6);
                G(v, m, 3, 4, 9, 14, r, 14);
                G(v, m, 2, 7, 8, 13, r, 12);
                G(v, m, 0, 5, 10, 15, r, 8);
                G(v, m, 1, 6, 11, 12, r, 10);
            }
        }

        private static void CompressScalar(Span<ulong> h, ReadOnlySpan<ulong> v)
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
            v[a] += v[b] + m[Sigma[p]];
            v[d] = RotateRight(v[d] ^ v[a], 32);
            v[c] += v[d];
            v[b] = RotateRight(v[b] ^ v[c], 24);
            v[a] += v[b] + m[Sigma[p + 1]];
            v[d] = RotateRight(v[d] ^ v[a], 16);
            v[c] += v[d];
            v[b] = RotateRight(v[b] ^ v[c], 63);

            static ulong RotateRight(ulong value, int nBits) =>
                (value >> nBits) | (value << (64 - nBits));
        }
    }
}
