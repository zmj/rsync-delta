using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            core.HashFinal(hash, isEndOfLayer: false);
        }

        public void Dispose() => _scratch.Dispose();
    }

    internal ref struct Blake2bCore
    {
        public const int ScratchSize = 448;

        private int _bufferFilled;
        private readonly Span<byte> _buf;
        private readonly Span<ulong> _v;
        private readonly Span<ulong> _m;
        private readonly Span<ulong> _h;
        private ulong _counter0;
        private ulong _counter1;
        private ulong _finalizationFlag0;
        private ulong _finalizationFlag1;

        private const int _numRounds = 12;
        private const int _blockSize = 128;

        private const ulong IV0 = 0x6A09E667F3BCC908UL;
        private const ulong IV1 = 0xBB67AE8584CAA73BUL;
        private const ulong IV2 = 0x3C6EF372FE94F82BUL;
        private const ulong IV3 = 0xA54FF53A5F1D36F1UL;
        private const ulong IV4 = 0x510E527FADE682D1UL;
        private const ulong IV5 = 0x9B05688C2B3E6C1FUL;
        private const ulong IV6 = 0x1F83D9ABFB41BD6BUL;
        private const ulong IV7 = 0x5BE0CD19137E2179UL;

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
            _buf = scratch.Slice(0, 128);

            _v = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(128, 128));
            _m = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(256, 128));
            _h = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(384, 64));

            _h[0] = IV0 ^ 0x01_01_00_20;
            _h[1] = IV1;
            _h[2] = IV2;
            _h[3] = IV3;
            _h[4] = IV4;
            _h[5] = IV5;
            _h[6] = IV6;
            _h[7] = IV7;

            _counter0 = 0;
            _counter1 = 0;
            _finalizationFlag0 = 0;
            _finalizationFlag1 = 0;
            _bufferFilled = 0;
        }

        public void HashCore(ReadOnlySpan<byte> array)
        {
            int start = 0;
            int count = array.Length;
            int offset = start;
            int bufferRemaining = _blockSize - _bufferFilled;

            if (_bufferFilled > 0 && count > bufferRemaining)
            {
                array.Slice(offset, bufferRemaining).CopyTo(
                    _buf.Slice(_bufferFilled));
                _counter0 += _blockSize;
                if (_counter0 == 0)
                    _counter1++;
                Compress(_buf, 0);
                offset += bufferRemaining;
                count -= bufferRemaining;
                _bufferFilled = 0;
            }

            while (count > _blockSize)
            {
                _counter0 += _blockSize; ;
                if (_counter0 == 0)
                    _counter1++;
                Compress(array, offset);
                offset += _blockSize;
                count -= _blockSize;
            }

            if (count > 0)
            {
                array.Slice(offset, count).CopyTo(
                    _buf.Slice(_bufferFilled));
                _bufferFilled += count;
            }
        }

        public void HashFinal(Span<byte> result, bool isEndOfLayer)
        {
            _counter0 += (uint)_bufferFilled;
            _finalizationFlag0 = ulong.MaxValue;
            if (isEndOfLayer)
                _finalizationFlag1 = ulong.MaxValue;
            for (int i = _bufferFilled; i < _buf.Length; i++)
                _buf[i] = 0;
            Compress(_buf, 0);

            Span<byte> hash = stackalloc byte[64];
            for (int i = 0; i < 8; ++i)
            {
                var buf = hash.Slice(i << 3);
                BinaryPrimitives.WriteUInt64LittleEndian(buf, _h[i]);
            }

            if (result.Length < hash.Length)
            {
                hash = hash.Slice(0, result.Length);
            }
            hash.CopyTo(result);
        }

        private static ulong RotateRight(ulong value, int nBits)
        {
            return (value >> nBits) | (value << (64 - nBits));
        }

        private void G(int a, int b, int c, int d, int r, int i)
        {
            int p = (r << 4) + i;
            _v[a] += _v[b] + _m[Sigma[p]];
            _v[d] = RotateRight(_v[d] ^ _v[a], 32);
            _v[c] += _v[d];
            _v[b] = RotateRight(_v[b] ^ _v[c], 24);
            _v[a] += _v[b] + _m[Sigma[p + 1]];
            _v[d] = RotateRight(_v[d] ^ _v[a], 16);
            _v[c] += _v[d];
            _v[b] = RotateRight(_v[b] ^ _v[c], 63);
        }

        private void Compress(ReadOnlySpan<byte> block, int start)
        {
            Debug.Assert(start == 0); // TODO research
            MemoryMarshal.Cast<byte, ulong>(block)
                .Slice(0, 16)
                .CopyTo(_m);

            _h.CopyTo(_v);
            _v[8] = IV0;
            _v[9] = IV1;
            _v[10] = IV2;
            _v[11] = IV3;
            _v[12] = IV4 ^ _counter0;
            _v[13] = IV5 ^ _counter1;
            _v[14] = IV6 ^ _finalizationFlag0;
            _v[15] = IV7 ^ _finalizationFlag1;

            for (int r = 0; r < _numRounds; ++r)
            {
                G(0, 4, 8, 12, r, 0);
                G(1, 5, 9, 13, r, 2);
                G(2, 6, 10, 14, r, 4);
                G(3, 7, 11, 15, r, 6);
                G(3, 4, 9, 14, r, 14);
                G(2, 7, 8, 13, r, 12);
                G(0, 5, 10, 15, r, 8);
                G(1, 6, 11, 12, r, 10);
            }

            for (int i = 0; i < 8; ++i)
            {
                _h[i] ^= _v[i] ^ _v[i + 8];
            }
        }
    }
}
