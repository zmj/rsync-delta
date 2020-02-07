using System;
using System.Buffers;
using System.Collections.Generic;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal sealed class BlockMatcher : IDisposable
    {
        public readonly SignatureOptions Options;
        private readonly Dictionary<BlockSignature, ulong> _blocks;
        private readonly IRollingHashAlgorithm _rollingHash;
        private readonly IStrongHashAlgorithm _strongHash;
        private readonly IMemoryOwner<byte> _strongHashBuffer;

        private BufferedBlock _block;
        private bool _recalculateStrongHash;

        public BlockMatcher(
            SignatureOptions options,
            Dictionary<BlockSignature, ulong> signatures,
            MemoryPool<byte> memoryPool)
        {
            Options = options;
            _rollingHash = HashAlgorithmFactory.Create(options.RollingHash);
            _strongHash = HashAlgorithmFactory.Create(options.StrongHash, memoryPool);
            _strongHashBuffer = memoryPool.Rent(options.StrongHashLength);
            _blocks = signatures;
        }

        public LongRange? MatchBlock(in BufferedBlock block)
        {
            BufferedBlock = block;
            var sig = new BlockSignature(this);
            return _blocks.TryGetValue(sig, out ulong start) ?
                new LongRange(start, (ulong)block.CurrentBlock.Length) :
                (LongRange?)null;
        }

        public int RollingHash => _rollingHash.Value;

        public ReadOnlyMemory<byte> StrongHash
        {
            get
            {
                if (_recalculateStrongHash)
                {
                    _strongHash.Hash(
                        _block.CurrentBlock,
                        _strongHashBuffer.Memory.Span);
                    _recalculateStrongHash = false;
                }
                return _strongHashBuffer.Memory;
            }
        }

        private BufferedBlock BufferedBlock
        {
            get => _block;
            set
            {
                _block = value;
                UpdateRollingHash();
                _recalculateStrongHash = true;
            }
        }

        private void UpdateRollingHash()
        {
            if (_block.PendingLiteral.IsEmpty)
            {
                _rollingHash.Initialize(_block.CurrentBlock);
            }
            else if (_block.CurrentBlock.Length == Options.BlockLength)
            {
                _rollingHash.Rotate(
                    remove: _block.PendingLiteral.LastByte(),
                    add: _block.CurrentBlock.LastByte());
            }
            else
            {
                _rollingHash.RotateOut(_block.PendingLiteral.LastByte());
            }
        }

        public void Dispose()
        {
            _strongHashBuffer.Dispose();
            _strongHash.Dispose();
        }
    }
}
