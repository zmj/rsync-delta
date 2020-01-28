#if !NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Rsync.Delta.Hash.Blake2b
{
    internal static unsafe class Blake2bAvx2
    {
        public const int ScratchLength = 0;

        public static void HashBlock(
            ReadOnlySpan<ulong> block,
            Span<ulong> hash,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag)
        {
            Debug.Assert(block.Length == 16);
            Debug.Assert(hash.Length == 8);

            Span<Vector256<ulong>> rows = stackalloc Vector256<ulong>[4];
            Init(hash, bytesHashed, bytesHashedOverflows, finalizationFlag, rows);
            var maskFF = MaskFF();
            var mask16 = Mask16();

            Span<Vector256<ulong>> permutedMsg = stackalloc Vector256<ulong>[4];
            fixed (ulong* msg = block)
            fixed (int* perm = Constants.MessagePermutation)
            {
                for (int r = 0; r < Constants.Rounds; r++)
                {
                    LoadMessage(r, msg, perm, maskFF, permutedMsg);
                    G(permutedMsg[0], permutedMsg[1], mask16, rows);
                    Diagonalize(rows);
                    G(permutedMsg[2], permutedMsg[3], mask16, rows);
                    Undiagonalize(rows);
                }
            }
            Compress(rows, hash);
        }

        private static void Init(
            ReadOnlySpan<ulong> hash,
            ulong bytesHashed,
            ulong bytesHashedOverflows,
            ulong finalizationFlag,
            Span<Vector256<ulong>> rows)
        {
            Debug.Assert(rows.Length == 4);
            fixed (ulong* h = hash)
            fixed (ulong* iv = Constants.IV)
            fixed (ulong* tmp = stackalloc ulong[4])
            {
                rows[0] = Avx.LoadVector256(h);
                rows[1] = Avx.LoadVector256(h + Vector256<ulong>.Count);
                rows[2] = Avx.LoadVector256(iv);
                rows[3] = Avx.LoadVector256(iv + Vector256<ulong>.Count);
                tmp[0] = bytesHashed;
                tmp[1] = bytesHashedOverflows;
                tmp[2] = finalizationFlag;
                rows[3] = Avx2.Xor(rows[3], Avx.LoadVector256(tmp));
            }
        }

        private static Vector256<ulong> MaskFF()
        {
            Vector256<ulong> mask = default;
            return Avx2.CompareEqual(mask, mask);
        }

        private static Vector256<byte> Mask16()
        {
            fixed (byte* b = Constants.ShuffleMask16)
            {
                return Avx2.BroadcastVector128ToVector256(b);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadMessage(
            int round,
            ulong* block,
            int* permutations,
            Vector256<ulong> ffMask,
            Span<Vector256<ulong>> permutedMsg)
        {
            Debug.Assert(permutedMsg.Length == 4);
            for (int i = 0; i < 4; i++)
            {
                var offset = round * 16 + i * Vector128<int>.Count;
                var permutation = Avx.LoadVector128(permutations + offset);
                permutedMsg[i] = Avx2.GatherMaskVector256(
                    source: default, // what does this do?
                    baseAddress: block,
                    index: permutation,
                    mask: ffMask,
                    scale: 8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void G(
            Vector256<ulong> msg1,
            Vector256<ulong> msg2,
            Vector256<byte> mask16,
            Span<Vector256<ulong>> rows)
        {
            Debug.Assert(rows.Length == 4);
            rows[0] = Avx2.Add(rows[0], msg1);
            rows[0] = Avx2.Add(rows[0], rows[1]);
            rows[3] = Avx2.Xor(rows[3], rows[0]);
            rows[3] = Avx2.Shuffle(rows[3].AsUInt32(), 0b_10_11_00_01).AsUInt64();

            rows[2] = Avx2.Add(rows[2], rows[3]);
            rows[1] = Avx2.Xor(rows[1], rows[2]);
            rows[1] = RotateRight(rows[1], 24);

            rows[0] = Avx2.Add(rows[0], msg2);
            rows[0] = Avx2.Add(rows[0], rows[1]);
            rows[3] = Avx2.Xor(rows[3], rows[0]);
            rows[3] = Avx2.Shuffle(rows[3].AsByte(), mask16).AsUInt64();

            rows[2] = Avx2.Add(rows[2], rows[3]);
            rows[1] = Avx2.Xor(rows[1], rows[2]);
            rows[1] = RotateRight(rows[1], 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> RotateRight(Vector256<ulong> v, byte n)
        {
            var tmp = Avx2.ShiftLeftLogical(v, (byte)(64 - n));
            v = Avx2.ShiftRightLogical(v, n);
            return Avx2.Xor(v, tmp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Diagonalize(Span<Vector256<ulong>> rows)
        {
            Debug.Assert(rows.Length == 4);
            rows[1] = Avx2.Permute4x64(rows[1], 0b_00_11_10_01);
            rows[2] = Avx2.Permute4x64(rows[2], 0b_01_00_11_10);
            rows[3] = Avx2.Permute4x64(rows[3], 0b_10_01_00_11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Undiagonalize(Span<Vector256<ulong>> rows)
        {
            Debug.Assert(rows.Length == 4);
            rows[1] = Avx2.Permute4x64(rows[1], 0b_10_01_00_11);
            rows[2] = Avx2.Permute4x64(rows[2], 0b_01_00_11_10);
            rows[3] = Avx2.Permute4x64(rows[3], 0b_00_11_10_01);
        }

        private static void Compress(
            Span<Vector256<ulong>> rows,
            Span<ulong> hash)
        {
            Debug.Assert(rows.Length == 4);
            Debug.Assert(hash.Length == 8);
            fixed (ulong* h = hash)
            {
                var h1 = Avx.LoadVector256(h);
                var h2 = Avx.LoadVector256(h + Vector256<ulong>.Count);
                h1 = Avx2.Xor(h1, rows[0]);
                h2 = Avx2.Xor(h2, rows[1]);
                h1 = Avx2.Xor(h1, rows[2]);
                h2 = Avx2.Xor(h2, rows[3]);
                Avx.Store(h, h1);
                Avx.Store(h + Vector256<ulong>.Count, h2);
            }
        }
    }
}
#endif
