using System;
using System.Buffers;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;

namespace Rsync.Delta.Delta
{
    internal sealed class BlockMatcher
        <TRollingHashAlgorithm, TStrongHashAlgorithm> : IDisposable
        where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
        where TStrongHashAlgorithm : IStrongHashAlgorithm
    {
        private readonly SignatureCollection<TRollingHashAlgorithm, TStrongHashAlgorithm> _signatures;
        private readonly int _blockLength;
        private readonly TRollingHashAlgorithm _rollingHashAlgorithm;
        private readonly TStrongHashAlgorithm _strongHashAlgorithm;
        private readonly IMemoryOwner<byte> _strongHashOwner;
        private readonly Memory<byte> _strongHashMemory;

        private ReadOnlySequence<byte> _sequence;
        private bool _recalculateStrongHash;

        public BlockMatcher(
            SignatureCollection<TRollingHashAlgorithm, TStrongHashAlgorithm> signatures,
            SignatureOptions options,
            TRollingHashAlgorithm rollingHashAlgorithm,
            TStrongHashAlgorithm strongHashAlgorithm,
            MemoryPool<byte> memoryPool)
        {
            _signatures = signatures;
            _blockLength = options.BlockLength;
            _rollingHashAlgorithm = rollingHashAlgorithm;
            _strongHashAlgorithm = strongHashAlgorithm;
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
            var block = new SlidingBlock<TRollingHashAlgorithm>
                (sequence, _blockLength, isFinalBlock, _rollingHashAlgorithm);
            while (block.TryAdvance(out var start, out var length, out var rollingHash))
            {
                _recalculateStrongHash = true;
                var sig = new BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (rollingHash, this, start, length);
                if (_signatures.TryGetValue(sig, out ulong matched))
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
                _strongHashAlgorithm.Hash(
                    _sequence.Slice(start, length),
                    _strongHashMemory.Span);
                _recalculateStrongHash = false;
            }
            return _strongHashMemory;
        }

        public void Dispose()
        {
            _strongHashOwner.Dispose();
            _strongHashAlgorithm.Dispose();
        }
    }
}
