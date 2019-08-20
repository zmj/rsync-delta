using System;
using System.Buffers;
using Rsync.Delta.Models;

namespace Rsync.Delta.Delta
{
    internal class LazyBlockSignature : IDisposable
    {
        // mutable, reused
        private readonly IMemoryOwner<byte>? _lease;

        public uint RollingHash {get;}

        public ReadOnlyMemory<byte> StrongHash {get;}

        public LazyBlockSignature(
            Func<ReadOnlyMemory<byte>> strongHash,
            uint rollingHash)
        {
            StrongHash = strongHash();
            RollingHash = rollingHash;
            _lease = null;
        }

        public void Dispose() => _lease?.Dispose();
    }
}