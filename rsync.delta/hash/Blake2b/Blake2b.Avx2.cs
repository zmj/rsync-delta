#if !NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash.Blake2b
{
    internal static unsafe class Blake2bAvx2
    {
        public static void HashBlock(
            ReadOnlySpan<ulong> block,
            Span<ulong> hash,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag)
        {
            Debug.Assert(block.Length == 16);
            Debug.Assert(hash.Length == 8);

            Init(hash, bytesHashed, bytesHashedOverflows, finalizationFlag,
                out var row1, out var row2, out var row3, out var row4);
            Vector256<ulong> ffMask = default;
            ffMask = Avx2.CompareEqual(ffMask, ffMask);
            for (int r = 0; r < Constants.Rounds; r++)
            {
                LoadMessage(r, block, ffMask,
                    out var tmp1, out var tmp2, out var tmp3, out var tmp4);
                G(tmp1, tmp2, ref row1, ref row2, ref row3, ref row4);
                Diagonalize(ref row2, ref row3, ref row4);
                G(tmp3, tmp4, ref row1, ref row2, ref row3, ref row4);
                Undiagonalize(ref row2, ref row3, ref row4);
            }
            Compress(hash, row1, row2, row3, row4);
        }

        private static void Init(
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
            fixed (ulong* iv = Constants.IV)
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

        private static void LoadMessage(
            int round,
            ReadOnlySpan<ulong> block,
            Vector256<ulong> ffMask,
            out Vector256<ulong> tmp1,
            out Vector256<ulong> tmp2,
            out Vector256<ulong> tmp3,
            out Vector256<ulong> tmp4)
        {
            fixed (int* sigma = Constants.MessagePermutation) // move up
            fixed (ulong* m = block)
            {
                var index1 = Avx.LoadVector128(sigma + round * 16);
                var index2 = Avx.LoadVector128(sigma + round * 16 + Vector128<int>.Count);
                var index3 = Avx.LoadVector128(sigma + round * 16 + 2 * Vector128<int>.Count);
                var index4 = Avx.LoadVector128(sigma + round * 16 + 3 * Vector128<int>.Count);

                tmp1 = Avx2.GatherMaskVector256(
                    source: default,
                    baseAddress: m, 
                    index1, 
                    mask: ffMask,
                    scale: 8);
                tmp2 = Avx2.GatherMaskVector256(
                    source: default,
                    baseAddress: m,
                    index2,
                    mask: ffMask,
                    scale: 8);
                tmp3 = Avx2.GatherMaskVector256(
                    source: default,
                    baseAddress: m,
                    index3,
                    mask: ffMask,
                    scale: 8);
                tmp4 = Avx2.GatherMaskVector256(
                    source: default,
                    baseAddress: m,
                    index4,
                    mask: ffMask,
                    scale: 8);
            }
        }

        private static void G(
            Vector256<ulong> buf1,
            Vector256<ulong> buf2,
            ref Vector256<ulong> row1,
            ref Vector256<ulong> row2,
            ref Vector256<ulong> row3,
            ref Vector256<ulong> row4)
        {
            row1 = Avx2.Add(row1, buf1);
            row1 = Avx2.Add(row1, row2);
            row4 = Avx2.Xor(row4, row1);
            row4 = Avx2.Shuffle(row4.AsUInt32(), 0b_10_11_00_01).AsUInt64();

            row3 = Avx2.Add(row3, row4);
            row2 = Avx2.Xor(row2, row3);
            row2 = RotateRight(row2, 24); // 25);

            row1 = Avx2.Add(row1, buf2);
            row1 = Avx2.Add(row1, row2);
            row4 = Avx2.Xor(row4, row1);

            Vector256<byte> mask16; // move this up
            fixed (byte* b = Constants.ShuffleMask16) { mask16 = Avx2.BroadcastVector128ToVector256(b); }
            var zz = RotateRight(row4, 16);
            row4 = Avx2.Shuffle(row4.AsByte(), mask16).AsUInt64();

            row3 = Avx2.Add(row3, row4);
            row2 = Avx2.Xor(row2, row3);
            row2 = RotateRight(row2, 63); // 11);
        }

        private static Vector256<ulong> RotateRight(Vector256<ulong> v, byte n)
        {
            var tmp = Avx2.ShiftLeftLogical(v, (byte)(64 - n));
            v = Avx2.ShiftRightLogical(v, n);
            return Avx2.Xor(v, tmp);
        }

        private static void Diagonalize(
            ref Vector256<ulong> row1,
            ref Vector256<ulong> row2,
            ref Vector256<ulong> row3)
        {
            row1 = Avx2.Permute4x64(row1, 0b_00_11_10_01);
            row2 = Avx2.Permute4x64(row2, 0b_01_00_11_10);
            row3 = Avx2.Permute4x64(row3, 0b_10_01_00_11);
        }

        private static void Undiagonalize(
            ref Vector256<ulong> row1,
            ref Vector256<ulong> row2,
            ref Vector256<ulong> row3)
        {
            row1 = Avx2.Permute4x64(row1, 0b_10_01_00_11);
            row2 = Avx2.Permute4x64(row2, 0b_01_00_11_10);
            row3 = Avx2.Permute4x64(row3, 0b_00_11_10_01);
        }

        private static void Compress(
            Span<ulong> hash,
            Vector256<ulong> row1,
            Vector256<ulong> row2,
            Vector256<ulong> row3,
            Vector256<ulong> row4)
        {
            fixed (ulong* h = hash)
            {
                var h1 = Avx.LoadVector256(h);
                var h2 = Avx.LoadVector256(h + Vector256<ulong>.Count);
                h1 = Avx2.Xor(h1, row1);
                h2 = Avx2.Xor(h2, row2);
                h1 = Avx2.Xor(h1, row3);
                h2 = Avx2.Xor(h2, row4);
                Avx.Store(h, h1);
                Avx.Store(h + Vector256<ulong>.Count, h2);
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

        static unsafe string Dbg(ulong z)
        {
            Span<ulong> zz = stackalloc ulong[1];
            zz[0] = z;
            return Dbg(zz);
        }
    }
}
#endif
