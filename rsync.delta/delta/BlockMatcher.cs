using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal partial class BlockMatcher : IDisposable
    {
        public readonly SignatureOptions Options;
        private readonly MemoryPool<byte> _memoryPool;
        
        private readonly Dictionary<BlockSignature, ulong> _blocks =
            new Dictionary<BlockSignature, ulong>();

        private readonly Func<ReadOnlyMemory<byte>> _lazyStrongHash;
        private ReadOnlySequence<byte> _currentBlock;
        private ReadOnlyMemory<byte> _currentBlockStrongHash;

        private RollingHash _rollingHash;

        public BlockMatcher(
            SignatureOptions options,
            //BlockSignature[] blockSignatures,
            MemoryPool<byte> memoryPool)
        {
            Options = options;
            _memoryPool = memoryPool;
            /*_blocks = new Dictionary<BlockSignature, ulong>(
                capacity: blockSignatures.Length);
            for (uint i= (uint)blockSignatures.Length-1; i<uint.MaxValue; i--)
            {
                _blocks[blockSignatures[i]] = (ulong)(i * options.BlockLength);
            }*/
            _rollingHash = new RollingHash();
            _lazyStrongHash = () => 
                _currentBlockStrongHash.Equals(default) ?
                    (_currentBlockStrongHash = CalculateStrongHash(_currentBlock)) :
                    _currentBlockStrongHash;
        }

        public void Dispose()
        {
            // todo: dispose hash scratch and calculated hashes
        }

        public void Add(BlockSignature sig, ulong start)
        {
            if (!_blocks.ContainsKey(sig))
            {
                _blocks.Add(sig, start);
            }
        }

        private ReadOnlyMemory<byte> CalculateStrongHash(ReadOnlySequence<byte> block)
        {
            var hash = new byte[Options.StrongHashLength];
            var scratch = new byte[Blake2bCore.ScratchSize];
            new Blake2b(_memoryPool).Hash(block, hash);
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
            else if (block.CurrentBlock.Length == Options.BlockLength)
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