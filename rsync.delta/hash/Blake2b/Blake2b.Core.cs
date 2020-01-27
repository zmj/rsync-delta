using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace Rsync.Delta.Hash.Blake2b
{
    /* Partial implementation of the blake2 hash algorithm.
	 * Based on the reference implementation at https://github.com/BLAKE2/BLAKE2
	 * Thanks to Christian Winnerlein and Dominik Reichl
     * for sharing their work as public domain.
	 */
    internal ref partial struct Blake2bCore
    {
        public const int ScratchSize = 320;
        private const int _blockLength = 128;

        private readonly Span<byte> _blockBuffer;
        private ReadOnlySpan<byte> _incompleteBlock;
        private Span<byte> _incompleteBlockRemainder =>
            _blockBuffer.Slice(_incompleteBlock.Length);

        private readonly Span<ulong> _v;
        private readonly Span<ulong> _h;
        private ulong _bytesHashed;
        private ulong _bytesHashedOverflows;

        public Blake2bCore(Span<byte> scratch)
        {
            Debug.Assert(scratch.Length >= ScratchSize);
            _blockBuffer = scratch.Slice(0, _blockLength);
            _v = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(128, 128));
            _h = MemoryMarshal.Cast<byte, ulong>(scratch.Slice(256, 64));

            Constants.IV.CopyTo(_h);
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
            if (data.Length <= remainder.Length)
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
            HashBlock(block, finalizationFlag: 0);
        }

        private void HashFinalBlock()
        {
            _bytesHashed += (uint)_incompleteBlock.Length;
            _incompleteBlockRemainder.Clear();
            HashBlock(_blockBuffer, finalizationFlag: ulong.MaxValue);
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

        private void HashBlock(ReadOnlySpan<byte> block, ulong finalizationFlag)
        {
            Debug.Assert(block.Length >= _blockLength);
            var m = MemoryMarshal.Cast<byte, ulong>(block.Slice(0, _blockLength));

#if !NETSTANDARD2_0
            if (Avx2.IsSupported)
            {
                Blake2bAvx2.HashBlock(
                    block: m,
                    hash: _h,
                    _bytesHashed,
                    _bytesHashedOverflows,
                    finalizationFlag);
            }
#else
            if (false)
            {
            }
#endif
            else
            {
                Blake2bScalar.HashBlock(
                    block: m,
                    scratch: _v,
                    hash: _h,
                    _bytesHashed,
                    _bytesHashedOverflows,
                    finalizationFlag);
            }
        }
    }
}
