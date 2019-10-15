using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace Rsync.Delta.Hash
{
    /* Partial implementation of the blake2 hash algorithm.
	 * Based on the reference implementation at https://github.com/BLAKE2/BLAKE2
	 * Special thanks to Christian Winnerlein and Dominik Reichl.
	 */
    internal readonly struct Blake2b : IDisposable
    {
        private readonly IMemoryOwner<byte> _scratch;

        public Blake2b(MemoryPool<byte> memoryPool)
        {
            _scratch = memoryPool.Rent(Blake2bCore.ScratchSize);
        }

        public void Hash(
            in ReadOnlySequence<byte> data,
            Span<byte> hash)
        {
            Debug.Assert(hash.Length <= 32);
            var core = new Blake2bCore(_scratch.Memory.Span);
            if (data.IsSingleSegment)
            {
                core.HashCore(data.First.Span);
            }
            else
            {
                foreach (var buffer in data)
                {
                    core.HashCore(buffer.Span);
                }
            }
            core.HashFinal(hash);
        }

        public void Dispose() => _scratch.Dispose();
    }

    internal ref struct Blake2bCore
    {
        public const int ScratchSize = 320;
        private const int _numRounds = 12;
        private const int _blockLength = 128;

        private readonly Span<byte> _blockBuffer;
        private ReadOnlySpan<byte> _incompleteBlock;
        private Span<byte> _incompleteBlockRemainder =>
            _blockBuffer.Slice(_incompleteBlock.Length);

        private readonly Span<ulong> _v;
        private readonly Span<ulong> _h;        
        private ulong _bytesHashed;
        private ulong _bytesHashedOverflows;

        private static readonly ulong[] IV = new ulong[8]
        {
            0x6A09E667F3BCC908UL,
            0xBB67AE8584CAA73BUL,
            0x3C6EF372FE94F82BUL,
            0xA54FF53A5F1D36F1UL,
            0x510E527FADE682D1UL,
            0x9B05688C2B3E6C1FUL,
            0x1F83D9ABFB41BD6BUL,
            0x5BE0CD19137E2179UL,
        };

        private static readonly int[] Sigma = new int[_numRounds * 16]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
            11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
            7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
            9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
            2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
            12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
            13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
            6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
            10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3
        };

        public Blake2bCore(Span<byte> scratch)
        {
            Debug.Assert(scratch.Length >= ScratchSize);
            _blockBuffer = scratch.Slice(0, _blockLength);
            _v = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(128, 128));
            _h = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(256, 64));

            IV.CopyTo(_h);
            _h[0] ^= 0x01_01_00_20;

            _bytesHashed = 0;
            _bytesHashedOverflows = 0;
            _incompleteBlock = Span<byte>.Empty;
        }

        public void HashCore(ReadOnlySpan<byte> data)
        {
            data = FinishIncompleteBlock(data);
            while (data.Length > _blockLength)
            {
                HashNonFinalBlock(data);
                data = data.Slice(_blockLength);
            }
            SaveIncompleteBlock(data);
        }

        private ReadOnlySpan<byte> FinishIncompleteBlock(ReadOnlySpan<byte> data)
        {
            if (_incompleteBlock.IsEmpty)
            {
                return data;
            }
            var remainder = _incompleteBlockRemainder;
            if (data.Length < remainder.Length)
            {
                return data;
            }
            data.Slice(0, remainder.Length).CopyTo(remainder);
            HashNonFinalBlock(_blockBuffer);
            _incompleteBlock = Span<byte>.Empty;
            return data.Slice(remainder.Length);
        }

        private void SaveIncompleteBlock(ReadOnlySpan<byte> data)
        {
            Debug.Assert(data.Length <= _incompleteBlockRemainder.Length);
            if (data.IsEmpty)
            {
                return;
            }
            data.CopyTo(_incompleteBlockRemainder);
            _incompleteBlock = _blockBuffer.Slice(
                start: 0, 
                length: _incompleteBlock.Length + data.Length);
        }

        private void HashNonFinalBlock(ReadOnlySpan<byte> block)
        {
            Debug.Assert(block.Length >= _blockLength);
            _bytesHashed += _blockLength;
            if (_bytesHashed == 0)
            {
                _bytesHashedOverflows++;
            }
            Compress(block, finalizationFlag: 0);
        }

        private void HashFinalBlock()
        {
            _bytesHashed += (uint)_incompleteBlock.Length;
            _incompleteBlockRemainder.Clear();
            Compress(_blockBuffer, finalizationFlag: ulong.MaxValue);
        }

        public void HashFinal(Span<byte> hash)
        {
            Debug.Assert(hash.Length <= 32);
            HashFinalBlock();

            if (hash.Length == 32)
            {
                Fill(hash, _h);
            }
            else
            {
                Span<byte> tmp = stackalloc byte[32];
                Fill(tmp, _h);
                tmp.Slice(0, hash.Length).CopyTo(hash);
            }

            static void Fill(Span<byte> buf, ReadOnlySpan<ulong> h)
            {
                Debug.Assert(buf.Length == 32);
                BinaryPrimitives.WriteUInt64LittleEndian(buf, h[0]);
                BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(8), h[1]);
                BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(16), h[2]);
                BinaryPrimitives.WriteUInt64LittleEndian(buf.Slice(24), h[3]);
            }
        }

        private void G(ReadOnlySpan<ulong> m, int a, int b, int c, int d, int r, int i)
        {
            int p = (r << 4) + i;
            _v[a] += _v[b] + m[Sigma[p]];
            _v[d] = RotateRight(_v[d] ^ _v[a], 32);
            _v[c] += _v[d];
            _v[b] = RotateRight(_v[b] ^ _v[c], 24);
            _v[a] += _v[b] + m[Sigma[p + 1]];
            _v[d] = RotateRight(_v[d] ^ _v[a], 16);
            _v[c] += _v[d];
            _v[b] = RotateRight(_v[b] ^ _v[c], 63);

            static ulong RotateRight(ulong value, int nBits) =>
                (value >> nBits) | (value << (64 - nBits));
        }

        private void Compress(ReadOnlySpan<byte> block, ulong finalizationFlag)
        {
            Debug.Assert(block.Length >= _blockLength);
            var m = MemoryMarshal.Cast<byte, ulong>(block.Slice(0, _blockLength));
            _h.CopyTo(_v);
            IV.CopyTo(_v.Slice(8));
            _v[12] ^= _bytesHashed;
            _v[13] ^= _bytesHashedOverflows;
            _v[14] ^= finalizationFlag;

#if !NETSTANDARD2_0
            if (Avx2.IsSupported)
            {
                RoundsAvx2(m);
                CompressAvx2();
            }
            else if (Sse2.IsSupported)
            {
                RoundsSse2(m);
                CompressSse2();
            }
#else
            if (false)
            {
            }
#endif
            else
            {
                RoundsScalar(m);
                CompressScalar();
            }
        }

        private void RoundsScalar(ReadOnlySpan<ulong> m)
        {
            for (int r = 0; r < _numRounds; ++r)
            {
                G(m, 0, 4, 8, 12, r, 0);
                G(m, 1, 5, 9, 13, r, 2);
                G(m, 2, 6, 10, 14, r, 4);
                G(m, 3, 7, 11, 15, r, 6);
                G(m, 3, 4, 9, 14, r, 14);
                G(m, 2, 7, 8, 13, r, 12);
                G(m, 0, 5, 10, 15, r, 8);
                G(m, 1, 6, 11, 12, r, 10);
            }
        }

        private void CompressScalar()
        {
            for (int i = 0; i < 8; ++i)
            {
                _h[i] ^= _v[i] ^ _v[i + 8];
            }
        }

#if !NETSTANDARD2_0
        private void RoundsSse2(ReadOnlySpan<ulong> m) // todo
        {
            for (int r = 0; r < _numRounds; ++r)
            {
                G(m, 0, 4, 8, 12, r, 0);
                G(m, 1, 5, 9, 13, r, 2);
                G(m, 2, 6, 10, 14, r, 4);
                G(m, 3, 7, 11, 15, r, 6);
                G(m, 3, 4, 9, 14, r, 14);
                G(m, 2, 7, 8, 13, r, 12);
                G(m, 0, 5, 10, 15, r, 8);
                G(m, 1, 6, 11, 12, r, 10);
            }
        }

        private unsafe void CompressSse2()
        {
            for (int i = 0; i < 8; i += Vector128<ulong>.Count)
            {
                Vector128<ulong> vLow;
                Vector128<ulong> vHigh;
                fixed (ulong* low = _v.Slice(i, Vector128<ulong>.Count))
                {
                    vLow = Sse2.LoadVector128(low);
                }
                fixed (ulong* high = _v.Slice(i + 8, Vector128<ulong>.Count))
                {
                    vHigh = Sse2.LoadVector128(high);
                }
                var vMixed = Sse2.Xor(vLow, vHigh);

                Vector128<ulong> hOld;
                fixed (ulong* h = _h.Slice(i))
                {
                    hOld = Sse2.LoadVector128(h);
                }
                var hNew = Sse2.Xor(vMixed, hOld);
                for (int j = 0; j < Vector128<ulong>.Count; j++)
                {
                    _h[i + j] = hNew.GetElement(j);
                }
            }
        }

        private void RoundsAvx2(ReadOnlySpan<ulong> m) // todo
        {
            for (int r = 0; r < _numRounds; ++r)
            {
                G(m, 0, 4, 8, 12, r, 0);
                G(m, 1, 5, 9, 13, r, 2);
                G(m, 2, 6, 10, 14, r, 4);
                G(m, 3, 7, 11, 15, r, 6);
                G(m, 3, 4, 9, 14, r, 14);
                G(m, 2, 7, 8, 13, r, 12);
                G(m, 0, 5, 10, 15, r, 8);
                G(m, 1, 6, 11, 12, r, 10);
            }
        }

        private unsafe void CompressAvx2()
        {
            for (int i = 0; i < 8; i += Vector256<ulong>.Count)
            {
                Vector256<ulong> vLow;
                Vector256<ulong> vHigh;
                fixed (ulong* low = _v.Slice(i, Vector256<ulong>.Count))
                {
                    vLow = Avx.LoadVector256(low);
                }
                fixed (ulong* high = _v.Slice(i + 8, Vector256<ulong>.Count))
                {
                    vHigh = Avx.LoadVector256(high);
                }
                var vMixed = Avx2.Xor(vLow, vHigh);

                Vector256<ulong> hOld;
                fixed (ulong* h = _h.Slice(i))
                {
                    hOld = Avx.LoadVector256(h);
                }
                var hNew = Avx2.Xor(vMixed, hOld);
                for (int j = 0; j < Vector256<ulong>.Count; j++)
                {
                    _h[i + j] = hNew.GetElement(j);
                }
            }
        }
#endif
    }
}
