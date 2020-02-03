using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Hash.Blake2b
{
    internal readonly struct Blake2b : IStrongHashAlgorithm
    {
        private readonly IMemoryOwner<byte> _scratch;

        public Blake2b(MemoryPool<byte> memoryPool)
        {
            _scratch = memoryPool.Rent(Constants.ScratchLength);
        }

        public void Hash(
            in ReadOnlySequence<byte> data,
            Span<byte> hash)
        {
            Debug.Assert(hash.Length <= 32);
            var core = new Blake2bCore(
                _scratch.Memory.Span.Slice(0, Constants.ScratchLength));
            if (data.IsSingleSegment)
            {
                core.HashCore(data.FirstSpan());
            }
            else
            {
                foreach (var buffer in data)
                {
                    core.HashCore(buffer.Span);
                }
            }
            core.HashFinal(hash);
        }

        public void Dispose() => _scratch.Dispose();
    }
}
