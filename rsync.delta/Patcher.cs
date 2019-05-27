using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

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
            var header = new DeltaHeader(new SequenceReader<byte>(readResult.Buffer));
            _reader.AdvanceTo(readResult.Buffer.GetPosition(DeltaHeader.Size));
        }

        private readonly struct Command
        {
            public readonly CopyCommand? Copy;
            public readonly LiteralCommand? Literal;

            public Command(CopyCommand copy) => (Copy, Literal) = (copy, null);
            public Command(LiteralCommand literal) => (Literal, Copy) = (literal, null);

            public const int MaxSize = CopyCommand.MaxSize > LiteralCommand.MaxSize ?
                CopyCommand.MaxSize : LiteralCommand.MaxSize;
        }

        private async ValueTask ExecuteCommands(CancellationToken ct)
        {
            while (true)
            {
                var readResult = await _reader.Buffer(Command.MaxSize, ct);
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
                else if (readResult.Buffer.FirstSpan[0] == 0) // END command
                {
                    return;
                }
                else
                {
                    throw new FormatException(nameof(Command));
                }
            }
        }
    }
}