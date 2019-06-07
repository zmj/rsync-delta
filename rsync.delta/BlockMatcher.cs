using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Rsync.Delta
{
    internal partial class BlockMatcher
    {
        public uint BlockLength => _options.BlockLength;
        private readonly Dictionary<BlockSignature, ulong> _blocks;

        private readonly Func<ReadOnlyMemory<byte>> _lazyStrongHash;
        private readonly SignatureOptions _options;
        
        private ReadOnlySequence<byte> _currentBlock;
        private ReadOnlyMemory<byte> _currentBlockStrongHash;

        private RollingHash _rollingHash;

        public BlockMatcher(
            SignatureOptions options,
            BlockSignature[] blockSignatures)
        {
            _options = options;
            _blocks = new Dictionary<BlockSignature, ulong>(
                capacity: blockSignatures.Length);
            for (uint i= (uint)blockSignatures.Length-1; i<uint.MaxValue; i--)
            {
                _blocks[blockSignatures[i]] = i * options.BlockLength;
            }
            _rollingHash = new RollingHash();
            _lazyStrongHash = () => 
                _currentBlockStrongHash.Equals(default) ?
                    (_currentBlockStrongHash = CalculateStrongHash(_currentBlock)) :
                    _currentBlockStrongHash;
        }

        private ReadOnlyMemory<byte> CalculateStrongHash(ReadOnlySequence<byte> block)
        {
            var hash = new byte[_options.StrongHashLength];
            Blake2.Blake2b.Hash(block, hash);
            return hash.AsMemory();
        }

        private Func<ReadOnlyMemory<byte>> LazyCalculateStrongHash(
            ReadOnlySequence<byte> block)
        {
            _currentBlock = block;
            _currentBlockStrongHash = default;
            return _lazyStrongHash;
        }

        public LongRange? MatchBlock(BufferedBlock block)
        {
            uint rollingHash = CalculateRollingHash(block);
            var strongHash = LazyCalculateStrongHash(block.CurrentBlock);
            var sig = new BlockSignature(rollingHash, strongHash);
            return _blocks.TryGetValue(sig, out ulong start) ? 
                new LongRange(start, (ulong)block.CurrentBlock.Length) : 
                (LongRange?)null;
        }

        private uint CalculateRollingHash(BufferedBlock block) 
        {
            if (block.PendingLiteral.IsEmpty)
            {
                _rollingHash = new RollingHash();
                _rollingHash.RotateIn(block.CurrentBlock);
            }
            else if (block.CurrentBlock.Length == BlockLength)
            {
                _rollingHash.Rotate(
                    remove: block.PendingLiteral.PeekLast(),
                    add: block.CurrentBlock.PeekLast());
            }
            else
            {
                _rollingHash.RotateOut(block.PendingLiteral.PeekLast());
            }
            return _rollingHash.Value;
        }
    }
}