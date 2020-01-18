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
            ReadOnlySpan<ulong> initializationVector,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag)
        {
            Debug.Assert(block.Length == 16);
            Debug.Assert(hash.Length == 8);
            fixed (ulong* m = block)
            fixed (ulong* h = hash)
            fixed (ulong* iv = initializationVector)
            {
                var row1 = Avx.LoadVector256(h);
                var row2 = Avx.LoadVector256(h + Vector256<ulong>.Count);
                var row3 = Avx.LoadVector256(iv);
                var row4 = Avx.LoadVector256(iv + Vector256<ulong>.Count);
                fixed (ulong* tmp = stackalloc ulong[4])
                {
                    tmp[0] = bytesHashed;
                    tmp[1] = bytesHashedOverflows;
                    tmp[2] = finalizationFlag;
                    row4 = Avx2.Xor(row4, Avx.LoadVector256(tmp));
                }



                string mm = Dbg(m, 16);
                var (r1, r2, r3, r4) = (Dbg(row1), Dbg(row2), Dbg(row3), Dbg(row4));
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
