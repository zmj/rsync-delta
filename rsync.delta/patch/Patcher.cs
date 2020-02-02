using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Patch
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
                await ReadHeader(ct).ConfigureAwait(false);
                await ExecuteCommands2(ct).ConfigureAwait(false);
                await _writer.FlushAsync(ct).ConfigureAwait(false);
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
            var header = await _reader.Read<DeltaHeader>(ct).ConfigureAwait(false);
            if (!header.HasValue)
            {
                throw new FormatException("failed to read delta header");
            }
        }

        private async ValueTask ExecuteCommands(CancellationToken ct)
        {
            FlushResult flushResult = default;
            while (!flushResult.IsCompleted)
            {
                CopyCommand? copy = await _reader.Read<CopyCommand>(ct).ConfigureAwait(false);
                if (copy.HasValue)
                {
                    flushResult = await _copier.WriteCopy(copy.Value.Range, ct).ConfigureAwait(false);
                    continue;
                }
                LiteralCommand? literal = await _reader.Read<LiteralCommand>(ct).ConfigureAwait(false);
                if (literal.HasValue)
                {
                    flushResult = await _writer.CopyFrom(
                        _reader,
                        (long)literal.Value.LiteralLength,
                        ct).ConfigureAwait(false);
                    continue;
                }
                EndCommand? end = await _reader.Read<EndCommand>(ct).ConfigureAwait(false);
                if (end.HasValue)
                {
                    return;
                }
                await ThrowUnknownCommand(ct).ConfigureAwait(false);
            }
        }

        private async ValueTask ExecuteCommands2(CancellationToken ct)
        {
            int maxCommandSize = default(CopyCommand).MaxSize;
            FlushResult flushResult = default;
            while (!flushResult.IsCompleted)
            {
                var readResult = await _reader.Buffer(maxCommandSize, ct).ConfigureAwait(false);
                if (readResult.Buffer.TryRead<CopyCommand>() is CopyCommand copy)
                {
                    _reader.AdvanceTo(readResult.Buffer.GetPosition(copy.Size));
                    flushResult = await _copier.WriteCopy(copy.Range, ct).ConfigureAwait(false);
                }
                else if (readResult.Buffer.TryRead<LiteralCommand>() is LiteralCommand literal)
                {
                    _reader.AdvanceTo(readResult.Buffer.GetPosition(literal.Size));
                    flushResult = await _writer.CopyFrom(
                        _reader,
                        (long)literal.LiteralLength,
                        ct).ConfigureAwait(false);
                }
                else if (readResult.Buffer.TryRead<EndCommand>() is EndCommand)
                {
                    return;
                }
                else
                {
                    string msg = readResult.Buffer.IsEmpty ?
                        "expected a command; got EOF" :
                        $"unknown command: {readResult.Buffer.FirstByte()}";
                    throw new FormatException(msg);
                }
            }
        }

        private async ValueTask ThrowUnknownCommand(CancellationToken ct)
        {
            var readResult = await _reader.Buffer(1, ct).ConfigureAwait(false);
            string msg = readResult.Buffer.IsEmpty ?
                "expected a command; got EOF" :
                $"unknown command: {readResult.Buffer.FirstByte()}";
            throw new FormatException(msg);
        }
    }
}
