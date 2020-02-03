using System;
using System.Buffers;
using Rsync.Delta.Hash.Adler;
using Rsync.Delta.Hash.Blake2b;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal class LazyBlockSignature : IDisposable
    {
        private readonly SignatureOptions _options;
        private readonly Adler32 _rollingHash;
        private readonly Blake2b _strongHash;
        private readonly IMemoryOwner<byte> _strongHashBuffer;

        private BufferedBlock _block;
        private bool _recalculateStrongHash;

        public LazyBlockSignature(
            SignatureOptions options, 
            MemoryPool<byte> pool)
        {
            _options = options;
            _rollingHash = new Adler32();
            _strongHash = new Blake2b(pool);
            _strongHashBuffer = pool.Rent(options.StrongHashLength);
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
