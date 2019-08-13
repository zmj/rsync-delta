using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta
{
    internal readonly struct SignatureReader
    {
        private readonly BlockMatcher.Builder _builder;
        private readonly PipeReader _reader;

        public SignatureReader(BlockMatcher.Builder builder, PipeReader reader)
        {
            _builder = builder;
            _reader = reader;
        }

        public async ValueTask<BlockMatcher> Read(CancellationToken ct)
        {
            try
            {
                var header = await ReadHeader(ct);
                _builder.Options = header.Options;
                await ReadBlockSignatures(header.Options.StrongHashLength, ct);
                _reader.Complete();
                return _builder.Build();
            }
            catch (Exception ex)
            {
                _reader.Complete(ex);
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
            int strongHashLength,
            CancellationToken ct)
        {
            uint size = BlockSignature.Size((ushort)strongHashLength);
            while (true)
            {
                var readResult = await _reader.Buffer(size, ct);
                var buffer = readResult.Buffer;
                if (buffer.Length == 0)
                {
                    return;
                }
                var sig = new BlockSignature(ref buffer, (int)strongHashLength);
                _reader.AdvanceTo(buffer.Start);
                _builder.Add(sig);
            }
        }
    }
}