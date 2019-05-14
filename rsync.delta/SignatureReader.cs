using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

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
                Console.WriteLine(header);
                // read blocks and add to builder
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
            return new SignatureHeader(readResult.Buffer);
        }
    }
}