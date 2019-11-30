#if !NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash
{
    internal ref partial struct Blake2bCore
    {
        private static unsafe void HashBlockAvx2(
            ReadOnlySpan<ulong> block,
            Span<ulong> scratch,
            Span<ulong> hash)
        {
            Debug.Assert(block.Length == 16);
            Debug.Assert(scratch.Length == 16);
            Debug.Assert(hash.Length == 8);
            fixed (ulong* v = scratch)
            {
                fixed (ulong* m = block)
                {
                    RoundsAvx2(v: scratch, m: block);
                }
                fixed (ulong* h = hash)
                {
                    CompressAvx2(v, h);
                }
            }
        }

        private static void RoundsAvx2(Span<ulong> v, ReadOnlySpan<ulong> m)
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

        private static unsafe void CompressAvx2(ulong* v, ulong* h)
        {
            for (int i = 0; i < 8; i += Vector256<ulong>.Count)
            {
                var low = Avx.LoadVector256(v + i);
                var high = Avx.LoadVector256(v + i + 8);
                var mixed = Avx2.Xor(low, high);

                var hOld = Avx.LoadVector256(h + i);
                var hNew = Avx2.Xor(mixed, hOld);
                Avx.Store(h + i, hNew);
            }
        }
    }
}
#endif
