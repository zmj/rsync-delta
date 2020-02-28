using System;
using System.Buffers;
using System.Collections.Generic;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;

namespace Rsync.Delta.Delta
{
    internal sealed class BlockMatcher : IDisposable
    {
        private readonly Dictionary<BlockSignature, ulong> _blocks;
        private readonly int _blockLength;
        private readonly IRollingHashAlgorithm _rollingHash;
        private readonly IStrongHashAlgorithm _strongHash;
        private readonly IMemoryOwner<byte> _strongHashOwner;
        private readonly Memory<byte> _strongHashMemory;

        private ReadOnlySequence<byte> _sequence;
        private bool _recalculateStrongHash;

        public BlockMatcher(
            SignatureOptions options,
            Dictionary<BlockSignature, ulong> signatures,
            MemoryPool<byte> memoryPool)
        {
            _blocks = signatures;
            _blockLength = options.BlockLength;
            _rollingHash = HashAlgorithmFactory.Create(options.RollingHash);
            _strongHash = HashAlgorithmFactory.Create(options.StrongHash, memoryPool);
            _strongHashOwner = memoryPool.Rent(options.StrongHashLength);
            _strongHashMemory = _strongHashOwner.Memory.Slice(0, options.StrongHashLength);            
        }

        public bool TryMatchBlock(
            in ReadOnlySequence<byte> sequence,
            bool isFinalBlock,
            out long matchStart,
            out LongRange match)
        {
            _sequence = sequence;
            var block = new SlidingBlock(sequence, _blockLength, isFinalBlock, _rollingHash);
            while (block.TryAdvance(out var start, out var length, out var rollingHash))
            {
                _recalculateStrongHash = true;
                var sig = new BlockSignature(rollingHash, this, start, length);
                if (_blocks.TryGetValue(sig, out ulong matched))
                {
                    matchStart = start;
                    match = new LongRange((long)matched, length);
                    return true;
                }
            }
            matchStart = default;
            match = default;
            return false;
        }

        public ReadOnlyMemory<byte> GetStrongHash(long start, long length)
        {
            if (_recalculateStrongHash)
            {
                _strongHash.Hash(
                    _sequence.Slice(start, length),
                    _strongHashMemory.Span);
                _recalculateStrongHash = false;
            }
            return _strongHashMemory;
        }

        public void Dispose()
        {
            _strongHashOwner.Dispose();
            _strongHash.Dispose();
        }
    }
}
