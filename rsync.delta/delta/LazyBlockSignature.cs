using System;
using System.Buffers;
using Rsync.Delta.Hash;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal class LazyBlockSignature : IDisposable
    {
        private readonly SignatureOptions _options;
        private readonly IRollingHashAlgorithm _rollingHash;
        private readonly IStrongHashAlgorithm _strongHash;
        private readonly IMemoryOwner<byte> _strongHashBuffer;

        private BufferedBlock _block;
        private bool _recalculateStrongHash;

        public LazyBlockSignature(
            SignatureOptions options,
            MemoryPool<byte> memoryPool)
        {
            _options = options;
            _rollingHash = HashAlgorithmFactory.Create(options.RollingHash);
            _strongHash = HashAlgorithmFactory.Create(options.StrongHash, memoryPool);
            _strongHashBuffer = memoryPool.Rent(options.StrongHashLength);
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

        public BufferedBlock Block
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
            else if (_block.CurrentBlock.Length == _options.BlockLength)
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
