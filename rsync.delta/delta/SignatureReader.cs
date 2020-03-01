using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal readonly struct SignatureReader
    {
        private readonly PipeReader _reader;
        private readonly MemoryPool<byte> _memoryPool;

        public SignatureReader(PipeReader reader, MemoryPool<byte> memoryPool)
        {
            _reader = reader;
            _memoryPool = memoryPool;
        }

        public async ValueTask<SignatureOptions> ReadHeader(CancellationToken ct)
        {
            var header = await _reader.Read<SignatureHeader>(ct).ConfigureAwait(false) ??
                throw new FormatException($"expected {nameof(SignatureHeader)}; got EOF");
            return header.Options;
        }

        public async ValueTask<SignatureCollection<TRollingHashAlgorithm, TStrongHashAlgorithm>> ReadSignatures
            <TRollingHashAlgorithm, TStrongHashAlgorithm>
                (SignatureOptions options,
                CancellationToken ct)
            where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
            where TStrongHashAlgorithm : IStrongHashAlgorithm
        {
            var signatures = new SignatureCollection<TRollingHashAlgorithm, TStrongHashAlgorithm>();
            const int maxSignatures = 1 << 22;
            for (int i = 0; i < maxSignatures; i++)
            {
                var sig = await _reader.Read
                    <BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm>, SignatureOptions>
                    (options, ct).ConfigureAwait(false);
                if (!sig.HasValue)
                {
                    _reader.Complete();
                    return signatures;
                }
                long start = options.BlockLength * i;
#if !NETSTANDARD2_0
                signatures.TryAdd(sig.Value, (ulong)start);
#else
                if (!signatures.ContainsKey(sig.Value))
                {
                    signatures.Add(sig.Value, (ulong)start);
                }
#endif
            }
            throw new FormatException($"too many signatures");
        }
    }
}
