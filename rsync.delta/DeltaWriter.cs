using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal readonly struct DeltaWriter
    {
        private readonly BlockMatcher _blocks;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;

        public DeltaWriter(BlockMatcher blocks, PipeReader reader, PipeWriter writer)
        {
            _blocks = blocks;
            _reader = reader;
            _writer = writer;
        }

        public async ValueTask Write(CancellationToken ct)
        {
            try
            {
                WriteHeader();
                await WriteCommands(ct);
                await _writer.FlushAsync(ct);
                _reader.Complete();
                _writer.Complete();
            }
            catch (Exception ex)
            {
                _reader.Complete(ex);
                _writer.Complete(ex);
                throw;
            }
        }

        private void WriteHeader()
        {
            var buffer = _writer.GetSpan(DeltaHeader.Size);
            new DeltaHeader().WriteTo(buffer);
            _writer.Advance(DeltaHeader.Size);
        }

        private async ValueTask WriteCommands(CancellationToken ct)
        {
            while (true)
            {
                var buffer = await TryReadBlock(ct);
                if (!buffer.HasValue)
                {
                    return;
                }
                // hm
            }
        }

        private async ValueTask<ReadOnlySequence<byte>?> TryReadBlock(
            CancellationToken ct)
        {
            var readResult = await _reader.Buffer(_blocks.BlockLength, ct);
            if (readResult.Buffer.Length == 0)
            {
                return null;
            }
            _reader.AdvanceTo(readResult.Buffer.End);
            return readResult.Buffer;
        }
    }
}