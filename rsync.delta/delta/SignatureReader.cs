using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
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

        public async ValueTask<(SignatureOptions, Dictionary<BlockSignature, ulong>)> Read(CancellationToken ct)
        {
            try
            {
                var header = await ReadHeader(ct).ConfigureAwait(false);
                var signatures = await ReadSignatures(header.Options, ct).ConfigureAwait(false);
                _reader.Complete();
                return (header.Options, signatures);
            }
            catch (Exception ex)
            {
                _reader.Complete(ex);
                throw;
            }
        }

        private async ValueTask<SignatureHeader> ReadHeader(CancellationToken ct) =>
            await _reader.Read<SignatureHeader>(ct).ConfigureAwait(false) ??
            throw new FormatException($"expected {nameof(SignatureHeader)}; got EOF");

        private async ValueTask<Dictionary<BlockSignature, ulong>> ReadSignatures(
            SignatureOptions options,
            CancellationToken ct)
        {
            var signatures = new Dictionary<BlockSignature, ulong>();
            const int maxSignatures = 1 << 22;
            for (int i = 0; i < maxSignatures; i++)
            {
                var sig = await _reader.Read<BlockSignature, SignatureOptions>(
                    options, ct).ConfigureAwait(false);
                if (!sig.HasValue)
                {
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
