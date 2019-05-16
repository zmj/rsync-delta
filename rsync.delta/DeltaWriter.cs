using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal class DeltaWriter
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
            LongRange? pendingCopy = null;
            while (true)
            {
                var buffer = await ReadBlock(ct);
                if (!buffer.HasValue)
                {
                    break;
                }
                LongRange? matched = _blocks.MatchBlock(
                    new SequenceReader<byte>(buffer.Value));
                if (!matched.HasValue)
                {
                    throw new NotImplementedException();
                }
                if (matched.Value.TryAppendTo(ref pendingCopy)) // ext method?
                {
                    // done
                }
                else
                {
                    WriteCopyCommand(pendingCopy!.Value);
                    pendingCopy = matched;
                }
                _reader.AdvanceTo(buffer.Value.End);
            }
            if (pendingCopy.HasValue)
            {
                WriteCopyCommand(pendingCopy.Value);
            }
        }

        private async ValueTask<ReadOnlySequence<byte>?> ReadBlock(
            CancellationToken ct)
        {
            var readResult = await _reader.Buffer(_blocks.BlockLength, ct);
            if (readResult.Buffer.IsEmpty)
            {
                return null;
            }
            return readResult.Buffer;
        }

        private void WriteCopyCommand(LongRange range)
        {
            var command = new CopyCommand(range);
            var buffer = _writer.GetSpan(command.Size);
            command.WriteTo(buffer);
            _writer.Advance(command.Size);
        }
    }
}