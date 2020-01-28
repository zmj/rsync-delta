using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using System.Runtime.Intrinsics.X86;
#endif

namespace Rsync.Delta.Hash.Blake2b
{
    /* Partial implementation of the blake2 hash algorithm.
	 * Based on the reference implementation at https://github.com/BLAKE2/BLAKE2
	 * Thanks to Christian Winnerlein and Dominik Reichl
     * for sharing their work as public domain.
	 */
    internal ref struct Blake2bCore
    {
        public const int BlockLength = 128;
        public const int MaxHashLength = 64;

        private readonly Span<byte> _blockBuffer;
        private ReadOnlySpan<byte> _incompleteBlock;
        private Span<byte> _incompleteBlockRemainder =>
            _blockBuffer.Slice(_incompleteBlock.Length);

        private readonly Span<ulong> _scratch;
        private readonly Span<ulong> _hash;
        private ulong _bytesHashed;
        private ulong _bytesHashedOverflows;

        public Blake2bCore(Span<byte> scratch)
        {
            Debug.Assert(scratch.Length == Constants.ScratchLength);

            _blockBuffer = scratch.Slice(0, BlockLength);
            scratch = scratch.Slice(BlockLength);

            var hash = scratch.Slice(0, MaxHashLength);
            _hash = MemoryMarshal.Cast<byte, ulong>(hash);
            scratch = scratch.Slice(MaxHashLength);

            _scratch = MemoryMarshal.Cast<byte, ulong>(scratch);

            Constants.IV.CopyTo(_hash);
            _hash[0] ^= 0x01_01_00_20;

            _bytesHashed = 0;
            _bytesHashedOverflows = 0;
            _incompleteBlock = Span<byte>.Empty;
        }

        public void HashCore(ReadOnlySpan<byte> data)
        {
            data = FinishIncompleteBlock(data);
            while (data.Length > BlockLength)
            {
                HashNonFinalBlock(data);
                data = data.Slice(BlockLength);
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
            Debug.Assert(block.Length >= BlockLength);
            _bytesHashed += BlockLength;
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
                Fill(hash, _hash);
            }
            else
            {
                Span<byte> tmp = stackalloc byte[32];
                Fill(tmp, _hash);
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
            Debug.Assert(block.Length >= BlockLength);
            var m = MemoryMarshal.Cast<byte, ulong>(block.Slice(0, BlockLength));

#if !NETSTANDARD2_0
            if (Avx2.IsSupported)
            {
                Blake2bAvx2.HashBlock(
                    block: m,
                    hash: _hash,
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
                    scratch: _scratch,
                    hash: _hash,
                    _bytesHashed,
                    _bytesHashedOverflows,
                    finalizationFlag);
            }
        }
    }
}
