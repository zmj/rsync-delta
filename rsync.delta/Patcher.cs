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
                await foreach (var command in ReadCommands(ct))
                {
                    await Execute(command, ct);
                }
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

        private async IAsyncEnumerable<Command> ReadCommands(CancellationToken ct)
        {
            while (true)
            {
                var readResult = await _reader.Buffer(Command.MaxSize, ct);
                if (readResult.Buffer.IsEmpty ||
                    readResult.Buffer.FirstSpan[0] == 0) // END command
                {
                    yield break;
                }
                if (CopyCommand.TryParse(readResult.Buffer, out var copy))
                {
                    _reader.AdvanceTo(readResult.Buffer.GetPosition(copy.Size));
                    yield return new Command(copy);
                }
                else if (LiteralCommand.TryParse(readResult.Buffer, out var literal))
                {
                    _reader.AdvanceTo(readResult.Buffer.GetPosition(literal.Size));
                    yield return new Command(literal);
                }
                else
                {
                    Console.WriteLine($"{BitConverter.ToString(readResult.Buffer.ToArray())}");
                    throw new FormatException(nameof(Command));
                }
            }
        }
        private async ValueTask Execute(Command command, CancellationToken ct)
        {
            if (command.Copy.HasValue)
            {
                await _copier.WriteCopy(command.Copy.Value.Range, ct);
            }
            else if (command.Literal.HasValue)
            {
                Console.WriteLine($"lit: {command.Literal.Value.LiteralLength}");
                await _reader.CopyTo(
                    _writer,
                    (long)command.Literal.Value.LiteralLength,
                    ct);
            }
        }
    }
}