using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal class SignatureReader
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
            var readResult = await _reader.Buffer(SignatureHeader.Size, ct);
            var header = new SignatureHeader(new SequenceReader<byte>(readResult.Buffer));
            _reader.AdvanceTo(readResult.Buffer.GetPosition(SignatureHeader.Size));
            return header;
        }

        private async ValueTask ReadBlockSignatures(
            uint strongHashLength,
            CancellationToken ct)
        {
            uint size = BlockSignature.Size(strongHashLength);
            while (true)
            {
                var readResult = await _reader.Buffer(size, ct);
                if (readResult.Buffer.Length == 0)
                {
                    return;
                }
                var sig = new BlockSignature(
                    new SequenceReader<byte>(readResult.Buffer),
                    strongHashLength);
                _builder.Add(sig);
                _reader.AdvanceTo(readResult.Buffer.GetPosition(size));
            }
        }
    }
}