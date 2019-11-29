#if !NETSTANDARD2_0
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash
{
    internal ref partial struct Blake2bCore
    {
        private static void RoundsSse2(Span<ulong> v, ReadOnlySpan<ulong> m) // todo
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

        private static unsafe void CompressSse2(Span<ulong> h, ReadOnlySpan<ulong> v)
        {
            for (int i = 0; i < 8; i += Vector128<ulong>.Count)
            {
                Vector128<ulong> vLow;
                Vector128<ulong> vHigh;
                fixed (ulong* low = v.Slice(i, Vector128<ulong>.Count))
                {
                    vLow = Sse2.LoadVector128(low);
                }
                fixed (ulong* high = v.Slice(i + 8, Vector128<ulong>.Count))
                {
                    vHigh = Sse2.LoadVector128(high);
                }
                var vMixed = Sse2.Xor(vLow, vHigh);

                Vector128<ulong> hOld;
                fixed (ulong* hPtr = h.Slice(i))
                {
                    hOld = Sse2.LoadVector128(hPtr);
                }
                var hNew = Sse2.Xor(vMixed, hOld);
                for (int j = 0; j < Vector128<ulong>.Count; j++)
                {
                    h[i + j] = hNew.GetElement(j);
                }
            }
        }
    }
}
#endif
