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
                await ExecuteCommands3(ct).ConfigureAwait(false);
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

        private async ValueTask ExecuteCommands3(CancellationToken ct)
        {
            FlushResult flushResult = default;
            while (!flushResult.IsCompleted)
            {
                var readResult = await _reader.ReadAsync(ct).ConfigureAwait(false);
                var opStatus = ReadCommand(
                    readResult.Buffer,
                    out var copyCommand,
                    out var literalCommand,
                    out var endCommand);
                if (opStatus == OperationStatus.Done)
                {
                    if (copyCommand is CopyCommand copy)
                    {
                        _reader.AdvanceTo(readResult.Buffer.GetPosition(copy.Size));
                        flushResult = await _copier.WriteCopy(copy.Range, ct).ConfigureAwait(false);
                    }
                    else if (literalCommand is LiteralCommand literal)
                    {
                        _reader.AdvanceTo(readResult.Buffer.GetPosition(literal.Size));
                        flushResult = await _writer.CopyFrom(
                            _reader,
                            (long)literal.LiteralLength,
                            ct).ConfigureAwait(false);
                    }
                    else if (endCommand is EndCommand end)
                    {
                        _reader.AdvanceTo(readResult.Buffer.GetPosition(end.Size));
                        return;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                else if (opStatus == OperationStatus.NeedMoreData)
                {
                    if (readResult.IsCompleted)
                    {
                        throw new FormatException("unexpected EOF");
                    }
                    _reader.AdvanceTo(
                        consumed: readResult.Buffer.Start,
                        examined: readResult.Buffer.End);
                }
                else if (opStatus == OperationStatus.InvalidData)
                {
                    throw new FormatException($"unknown command: {readResult.Buffer.FirstByte()}");
                }
                else
                {
                    throw new ArgumentException($"unexpected {nameof(OperationStatus)}.{opStatus}");
                }
            }
        }

        private static OperationStatus ReadCommand(
            in ReadOnlySequence<byte> sequence,
            out CopyCommand? copyCommand,
            out LiteralCommand? literalCommand,
            out EndCommand? endCommand)
        {
            copyCommand = null;
            literalCommand = null;
            endCommand = null;
            var copyStatus = sequence.Read(out CopyCommand copy);
            if (copyStatus == OperationStatus.Done)
            {
                copyCommand = copy;
                return OperationStatus.Done;
            }
            var literalStatus = sequence.Read(out LiteralCommand literal);
            if (literalStatus == OperationStatus.Done)
            {
                literalCommand = literal;
                return OperationStatus.Done;
            }
            var endStatus = sequence.Read(out EndCommand end);
            if (endStatus == OperationStatus.Done)
            {
                endCommand = end;
                return OperationStatus.Done;
            }
            return copyStatus < literalStatus ?
                copyStatus < endStatus ? copyStatus : endStatus :
                literalStatus < endStatus ? literalStatus : endStatus;
        }
    }
}
