﻿#if !NETSTANDARD2_0
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
            LoadMessage(block, out var msg1, out var msg2, out var msg3, out var msg4);
            for (int r = 0; r < _numRounds; r++)
            {
                MixMessage(r, msg1, msg2, msg3, msg4,
                    out var tmp1, out var tmp2, out var tmp3, out var tmp4);
                G(tmp1, tmp2, ref row1, ref row2, ref row3, ref row4);
                Diagonalize(ref row1, ref row2, ref row3);
                G(tmp3, tmp4, ref row1, ref row2, ref row4, ref row4);
                Undiagonalize(ref row1, ref row2, ref row3);
            }
            //var (r1, r2, r3, r4) = (Dbg(row1), Dbg(row2), Dbg(row3), Dbg(row4));
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

        private static unsafe void LoadMessage(
            ReadOnlySpan<ulong> block,
            out Vector256<ulong> msg1,
            out Vector256<ulong> msg2,
            out Vector256<ulong> msg3,
            out Vector256<ulong> msg4)
        {
            fixed (ulong* m = block)
            {
                msg1 = Avx.LoadVector256(m);
                msg2 = Avx.LoadVector256(m + Vector256<ulong>.Count);
                msg3 = Avx.LoadVector256(m + 2 * Vector256<ulong>.Count);
                msg4 = Avx.LoadVector256(m + 3 * Vector256<ulong>.Count);
            }
        }

        private static unsafe void MixMessage(
            int round,
            Vector256<ulong> msg1,
            Vector256<ulong> msg2,
            Vector256<ulong> msg3,
            Vector256<ulong> msg4,
            out Vector256<ulong> tmp1,
            out Vector256<ulong> tmp2,
            out Vector256<ulong> tmp3,
            out Vector256<ulong> tmp4)
        {
            throw new NotImplementedException();
        }

        private static readonly byte[] _mask16 = new byte[]
        {
            2, 3, 4, 5, 6, 7, 0, 1,
            10, 11, 12, 13, 14, 15, 8, 9,
        };

        private static unsafe void G(
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
            row2 = Rotate(row2, 25);

            row1 = Avx2.Add(row1, buf2);
            row1 = Avx2.Add(row1, row2);
            row4 = Avx2.Xor(row4, row1);

            Vector256<byte> mask16; // move this up
            fixed (byte* b = _mask16) { mask16 = Avx.LoadVector256(b); }
            row4 = Avx2.Shuffle(row4.AsByte(), mask16).AsUInt64();

            row3 = Avx2.Add(row3, row4);
            row2 = Avx2.Xor(row2, row3);
            row2 = Rotate(row2, 11);
        }

        private static unsafe Vector256<ulong> Rotate(Vector256<ulong> v, byte n)
        {
            var tmp = Avx2.ShiftLeftLogical(v, (byte)(64 - n));
            v = Avx2.ShiftRightLogical(v, n);
            return Avx2.Xor(v, tmp);
        }

        private static unsafe void Diagonalize(
            ref Vector256<ulong> row1,
            ref Vector256<ulong> row2,
            ref Vector256<ulong> row3)
        {
            row1 = Avx2.Permute4x64(row1, 0b_00_11_10_01);
            row2 = Avx2.Permute4x64(row2, 0b_01_00_11_10);
            row3 = Avx2.Permute4x64(row3, 0b_10_01_00_11);
        }

        private static unsafe void Undiagonalize(
            ref Vector256<ulong> row1,
            ref Vector256<ulong> row2,
            ref Vector256<ulong> row3)
        {
            row1 = Avx2.Permute4x64(row1, 0b_10_01_00_11);
            row2 = Avx2.Permute4x64(row2, 0b_01_00_11_10);
            row3 = Avx2.Permute4x64(row3, 0b_00_11_10_01);
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
    }
}
#endif