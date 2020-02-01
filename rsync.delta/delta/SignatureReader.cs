using System;
using System.Buffers;
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

        public async ValueTask<BlockMatcher> Read(CancellationToken ct)
        {
            BlockMatcher? matcher = null;
            try
            {
                var header = await ReadHeader(ct).ConfigureAwait(false);
                matcher = new BlockMatcher(header.Options, _memoryPool);
                await ReadBlockSignatures(matcher.Value, ct).ConfigureAwait(false);
                _reader.Complete();
                return matcher.Value;
            }
            catch (Exception ex)
            {
                _reader.Complete(ex);
                matcher?.Dispose();
                throw;
            }
        }

        private async ValueTask<SignatureHeader> ReadHeader(CancellationToken ct) =>
            await _reader.Read2<SignatureHeader>(ct).ConfigureAwait(false) ??
            throw new FormatException($"expected {nameof(SignatureHeader)}; got EOF");

        private async ValueTask ReadBlockSignatures(
            BlockMatcher matcher,
            CancellationToken ct)
        {
            const int maxSignatures = 1 << 22;
            for (int i = 0; i < maxSignatures; i++)
            {
                var sig = await _reader.Read2<BlockSignature, SignatureOptions>(
                    matcher.Options, ct).ConfigureAwait(false);
                if (!sig.HasValue)
                {
                    return;
                }
                long start = matcher.Options.BlockLength * i;
                matcher.Add(sig.Value, (ulong)start);
            }
            throw new FormatException($"too many signatures");
        }
    }
}
