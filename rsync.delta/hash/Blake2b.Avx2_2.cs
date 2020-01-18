#if !NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash
{
    internal ref partial struct Blake2bCore
    {
        private static unsafe void HashBlockAvx2_2(
            ReadOnlySpan<ulong> block,
            Span<ulong> hash,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag)
        {
            Debug.Assert(block.Length == 16);
            Debug.Assert(hash.Length == 8);

            InitAvx2(hash, bytesHashed, bytesHashedOverflows, finalizationFlag,
                out var row1, out var row2, out var row3, out var row4);
            fixed (ulong* m = block)
            {
                for (int r = 0; r < _numRounds; r++)
                {
                    // round
                }
            }
            var (r1, r2, r3, r4) = (Dbg(row1), Dbg(row2), Dbg(row3), Dbg(row4));
            CompressAvx2(hash, row1, row2, row3, row4);
        }

        private static unsafe void InitAvx2(
            ReadOnlySpan<ulong> hash,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag,
            out Vector256<ulong> row1,
            out Vector256<ulong> row2,
            out Vector256<ulong> row3,
            out Vector256<ulong> row4)
        {
            fixed (ulong* h = hash)
            fixed (ulong* iv = IV)
            fixed (ulong* tmp = stackalloc ulong[4])
            {
                row1 = Avx.LoadVector256(h);
                row2 = Avx.LoadVector256(h + Vector256<ulong>.Count);
                row3 = Avx.LoadVector256(iv);
                row4 = Avx.LoadVector256(iv + Vector256<ulong>.Count);
                tmp[0] = bytesHashed;
                tmp[1] = bytesHashedOverflows;
                tmp[2] = finalizationFlag;
                row4 = Avx2.Xor(row4, Avx.LoadVector256(tmp));
            }
        }

        private static unsafe void CompressAvx2(
            Span<ulong> hash,
            Vector256<ulong> row1,
            Vector256<ulong> row2,
            Vector256<ulong> row3,
            Vector256<ulong> row4)
        {
            fixed (ulong* h = hash)
            {
                // compress
                throw new NotImplementedException();
            }
        }

        static string Dbg(ReadOnlySpan<ulong> z) =>
            BitConverter.ToString(
                MemoryMarshal.Cast<ulong, byte>(z)
                .ToArray());

        static unsafe string Dbg(ulong* z, int len) =>
            Dbg(new Span<ulong>(z, len));

        static unsafe string Dbg(Vector256<ulong> z)
        {
            ulong* zz = stackalloc ulong[Vector256<ulong>.Count];
            Avx.Store(zz, z);
            return Dbg(zz, Vector256<ulong>.Count);
        }
    }
}
#endif
