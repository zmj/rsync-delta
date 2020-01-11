using System;
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
            var header = await _reader.Read<DeltaHeader>(ct);
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
                CopyCommand? copy = await _reader.Read<CopyCommand>(ct);
                if (copy.HasValue)
                {
                    flushResult = await _copier.WriteCopy(copy.Value.Range, ct);
                    continue;
                }
                LiteralCommand? literal = await _reader.Read<LiteralCommand>(ct);
                if (literal.HasValue)
                {
                    flushResult = await _writer.CopyFrom(
                        _reader,
                        (long)literal.Value.LiteralLength,
                        ct);
                    continue;
                }
                EndCommand? end = await _reader.Read<EndCommand>(ct);
                if (end.HasValue)
                {
                    return;
                }
                throw new FormatException("unknown command");
            }
        }
    }
}