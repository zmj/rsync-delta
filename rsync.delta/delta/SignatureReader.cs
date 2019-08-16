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
                var header = await ReadHeader(ct);
                matcher = new BlockMatcher(header.Options, _memoryPool);
                await ReadBlockSignatures(matcher, ct);
                _reader.Complete();
                return matcher;
            }
            catch (Exception ex)
            {
                _reader.Complete(ex);
                matcher?.Dispose();
                throw;
            }
        }

        private async ValueTask<SignatureHeader> ReadHeader(CancellationToken ct)
        {
            var readResult = await _reader.Buffer(new SignatureHeader().Size, ct);
            var buffer = readResult.Buffer;
            var header = new SignatureHeader(ref buffer);
            _reader.AdvanceTo(buffer.Start);
            return header;
        }

        private async ValueTask ReadBlockSignatures(
            BlockMatcher matcher,
            CancellationToken ct)
        {
            int blockLength = matcher.Options!.BlockLength;
            int strongHashLength = matcher.Options!.StrongHashLength;
            uint size = BlockSignature.SSize((ushort)strongHashLength);
            for (int i = 0; ; i++)
            {
                var readResult = await _reader.Buffer(size, ct);
                var buffer = readResult.Buffer;
                if (buffer.Length == 0)
                {
                    return;
                }
                var sig = new BlockSignature(ref buffer, (int)strongHashLength);
                _reader.AdvanceTo(buffer.Start);
                long start = blockLength * i; // todo checked
                matcher.Add(sig, (ulong)start);
            }
        }
    }
}