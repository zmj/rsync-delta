using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Pipes;

namespace Rsync.Delta
{
    internal readonly struct Patcher
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private readonly Copier _copier;

        public Patcher(PipeReader reader, PipeWriter writer, Copier copier)
        {
            _reader = reader;
            _writer = writer;
            _copier = copier;
        }

        public async ValueTask Patch(CancellationToken ct)
        {
            try
            {
                await ReadHeader(ct);
                await ExecuteCommands(ct);
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

        private async ValueTask ReadHeader(CancellationToken ct)
        {
            var readResult = await _reader.Buffer(DeltaHeader.Size, ct);
            var buffer = readResult.Buffer;
            var header = new DeltaHeader(ref buffer);
            _reader.AdvanceTo(buffer.Start);
        }

        private async ValueTask ExecuteCommands(CancellationToken ct)
        {
            const int maxCommandSize = CopyCommand.MaxSize > LiteralCommand.MaxSize ?
                CopyCommand.MaxSize : LiteralCommand.MaxSize;
            while (true)
            {
                var readResult = await _reader.Buffer(maxCommandSize, ct);
                if (readResult.Buffer.IsEmpty)
                {
                    throw new FormatException("Delta ended without end command");
                }
                if (CopyCommand.TryParse(readResult.Buffer, out var copy))
                {
                    _reader.AdvanceTo(readResult.Buffer.GetPosition(copy.Size));
                    await _copier.WriteCopy(copy.Range, ct);
                }
                else if (LiteralCommand.TryParse(readResult.Buffer, out var literal))
                {
                    _reader.AdvanceTo(readResult.Buffer.GetPosition(literal.Size));
                    await _reader.CopyTo(
                        _writer,
                        (long)literal.LiteralLength,
                        ct);
                }
                else if (readResult.Buffer.Length == 1 &&
                    readResult.Buffer.First.Span[0] == 0) // END command
                {
                    return;
                }
                else
                {
                    throw new FormatException("unknown command");
                }
            }
        }
    }
}