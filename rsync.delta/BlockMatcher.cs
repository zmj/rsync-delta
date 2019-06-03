using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Rsync.Delta
{
    internal partial class BlockMatcher
    {
        public readonly uint BlockLength;
        private readonly Dictionary<BlockSignature, ulong> _blocks;

        private readonly RollingHash _rollingHash;
        private readonly Func<ReadOnlyMemory<byte>> _lazyStrongHash;
        
        private ReadOnlySequence<byte> _currentBlock;
        private ReadOnlyMemory<byte> _currentBlockStrongHash;

        public BlockMatcher(
            SignatureOptions options,
            BlockSignature[] blockSignatures)
        {
            BlockLength = options.BlockLength;
            _blocks = new Dictionary<BlockSignature, ulong>(
                capacity: blockSignatures.Length);
            for (uint i= (uint)blockSignatures.Length-1; i<uint.MaxValue; i--)
            {
                _blocks[blockSignatures[i]] = i * options.BlockLength;
            }
            _rollingHash = new RollingHash(options.BlockLength);
            _lazyStrongHash = () => 
                _currentBlockStrongHash.Equals(default) ?
                    (_currentBlockStrongHash = CalculateStrongHash(_currentBlock)) :
                    _currentBlockStrongHash;
        }

        private static ReadOnlyMemory<byte> CalculateStrongHash(ReadOnlySequence<byte> block)
        {
            byte[] buffer = block.ToArray();
            byte[] hash = Blake2.Blake2b.Hash(buffer);
            return hash.AsMemory();
        }

        public LongRange? MatchBlock(BufferedBlock block)
        {
            uint rollingHash = CalculateRollingHash(block);

            _currentBlock = block.CurrentBlock;
            _currentBlockStrongHash = default;
            var sig = new BlockSignature(rollingHash, _lazyStrongHash);
            return _blocks.TryGetValue(sig, out ulong start) ? 
                new LongRange(start, (ulong)block.CurrentBlock.Length) : 
                (LongRange?)null;
        }

        private uint CalculateRollingHash(BufferedBlock block) =>
            block.PendingLiteral.IsEmpty ?
                _rollingHash.Hash(block.CurrentBlock) :
                _rollingHash.Rotate(
                    rollOut: block.PendingLiteral.PeekLast(),
                    rollIn: block.CurrentBlock.PeekLast());
    }
}