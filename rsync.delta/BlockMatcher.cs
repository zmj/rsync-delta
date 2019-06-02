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

        public LongRange? MatchBlock(ReadOnlySequence<byte> buffer3)
        {
            // roll the rolling hash
            uint rollingHash = 0;
            
            _currentBlock = buffer3;
            _currentBlockStrongHash = default;
            var sig = new BlockSignature(rollingHash, _lazyStrongHash);
            return _blocks.TryGetValue(sig, out ulong start) ? 
                new LongRange(start, (ulong)buffer3.Length) : 
                (LongRange?)null;
        }
    }
}