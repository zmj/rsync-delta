#if !NETSTANDARD2_0
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash
{
    internal ref partial struct Blake2bCore
    {
        private static void RoundsAvx2(Span<ulong> v, ReadOnlySpan<ulong> m) // todo
        {
            for (int r = 0; r < _numRounds; ++r)
            {
                ColumnStep(v, m, r, 0);

                // rotate left
                for (int i = 0; i < 4; i++)
                {
                    Span<ulong> row = v.Slice(i * 4, 4);
                    Span<ulong> tmp = stackalloc ulong[4];
                    row[..i].CopyTo(tmp[^i..]);
                    row[i..].CopyTo(tmp);
                    tmp.CopyTo(row);
                }

                ColumnStep(v, m, r, 8);

                // rotate right
                for (int i = 0; i < 4; i++)
                {
                    Span<ulong> row = v.Slice(i * 4, 4);
                    Span<ulong> tmp = stackalloc ulong[4];
                    row[..^i].CopyTo(tmp[i..]);
                    row[^i..].CopyTo(tmp);
                    tmp.CopyTo(row);
                }
            }

            static void ColumnStep(Span<ulong> v, ReadOnlySpan<ulong> m, int r, int o)
            {
                G(v, m, 0, 4, 8, 12, r, 0 + o);
                G(v, m, 1, 5, 9, 13, r, 2 + o);
                G(v, m, 2, 6, 10, 14, r, 4 + o);
                G(v, m, 3, 7, 11, 15, r, 6 + o);
            }
        }

        private static unsafe void CompressAvx2(Span<ulong> h, ReadOnlySpan<ulong> v)
        {
            for (int i = 0; i < 8; i += Vector256<ulong>.Count)
            {
                Vector256<ulong> vLow;
                Vector256<ulong> vHigh;
                fixed (ulong* low = v.Slice(i, Vector256<ulong>.Count))
                {
                    vLow = Avx.LoadVector256(low);
                }
                fixed (ulong* high = v.Slice(i + 8, Vector256<ulong>.Count))
                {
                    vHigh = Avx.LoadVector256(high);
                }
                var vMixed = Avx2.Xor(vLow, vHigh);

                Vector256<ulong> hOld;
                fixed (ulong* hPtr = h.Slice(i))
                {
                    hOld = Avx.LoadVector256(hPtr);
                }
                var hNew = Avx2.Xor(vMixed, hOld);
                for (int j = 0; j < Vector256<ulong>.Count; j++)
                {
                    h[i + j] = hNew.GetElement(j);
                }
            }
        }
    }
}
#endif
