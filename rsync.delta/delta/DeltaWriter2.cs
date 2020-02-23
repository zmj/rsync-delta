using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal readonly struct DeltaWriter2
    {
        private readonly BlockMatcher _matcher;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private const int _flushThreshhold = 1 << 12;

        public DeltaWriter2(
            BlockMatcher matcher,
            PipeReader reader,
            PipeWriter writer)
        {
            _matcher = matcher;
            _reader = reader;
            _writer = writer;
        }

        public async ValueTask Write(CancellationToken ct)
        {
            try
            {
                _writer.Write(new DeltaHeader());
                await WriteCommands(ct).ConfigureAwait(false);
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

        private async ValueTask WriteCommands(CancellationToken ct)
        {
            long pendingLiteral = 0;
            LongRange pendingCopy = default;
            int writtenAfterFlush = new DeltaHeader().Size;
            FlushResult flushResult = default;
            while (!flushResult.IsCompleted)
            {
                var readResult = await _reader.ReadAsync(ct).ConfigureAwait(false);
                if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
                {
                    // write any pending stuff? or is that already done?
                    // write the final literal on !matched && isCompleted
                    _writer.Write(new EndCommand());
                    return;
                }
                while (true)
                {
                    if (_matcher.TryMatchBlock(
                        readResult.Buffer.Slice(pendingLiteral),
                        isFinalBlock: readResult.IsCompleted,
                        out long matchStart,
                        out LongRange match))
                    {
                        // flush literal: PLL + matchStart
                        if (pendingCopy.TryAppend(match, out var appended))
                        {
                            pendingCopy = appended;
                        }
                        else
                        {
                            writtenAfterFlush += WriteCopy(pendingCopy);
                            pendingCopy = match;
                        }
                        var consumed = pendingLiteral + matchStart + match.Length;
                        _reader.AdvanceTo(readResult.Buffer.GetPosition(consumed));
                        continue;
                    }
                    // else not matched
                    // if eof - write the final literal
                }
                _reader.AdvanceTo(consumed, examined);

                if (writtenAfterFlush >= _flushThreshhold)
                {
                    flushResult = await _writer.FlushAsync(ct).ConfigureAwait(false);
                    writtenAfterFlush = 0;
                }
            }
        }

        private int WriteCopy(LongRange range)
        {
            if (range.Length == 0)
            {
                return 0;
            }
            return _writer.Write(new CopyCommand(range));
        }

        private int WriteLiteral(in ReadOnlySequence<byte> sequence)
        {
            // write in loop
            throw new NotImplementedException();
        }
    }
}
