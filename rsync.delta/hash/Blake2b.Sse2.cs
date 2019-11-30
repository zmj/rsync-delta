#if !NETSTANDARD2_0
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash
{
    internal ref partial struct Blake2bCore
    {
        private static unsafe void HashBlockSse2(
            ReadOnlySpan<ulong> block,
            Span<ulong> scratch,
            Span<ulong> hash)
        {
            RoundsScalar(v: scratch, m: block); // todo
            CompressSse2(h: hash, v: scratch);
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
