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
    internal sealed class DeltaWriter2
    {
        private readonly BlockMatcher _matcher;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private const int _flushThreshhold = 1 << 12;

        private LongRange _pendingCopyRange;
        private int _writtenAfterFlush;

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
                _writtenAfterFlush = _writer.Write(new DeltaHeader());
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
            FlushResult flushResult = default;
            while (!flushResult.IsCompleted)
            {
                var readResult = await _reader.ReadAsync(ct).ConfigureAwait(false);
                if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
                {
                    // write any pending stuff? or is that already done?
                    _writtenAfterFlush += _writer.Write(new EndCommand());
                    return;
                }
                long pendingLiteralLength = 0;
                while (true)
                {
                    // where is ROS sliced past examined bytes?
                    // tentative: in deltaWriter, after this call, before Advance
                    // this allows a pre-sliced ROS to be passed into here
                    var opStatus = _matcher.MatchBlock(
                        readResult.Buffer.Slice(pendingLiteralLength),
                        readResult.IsCompleted,
                        out LongRange? matched,
                        out long consumed);
                    if (opStatus == OperationStatus.NeedMoreData)
                    {
                        _reader.AdvanceTo(
                            consumed: readResult.Buffer.Start,
                            examined: readResult.Buffer.End);
                        break;
                    }
                    Debug.Assert(opStatus == OperationStatus.Done);
                    // todo - how is consumed updated? int or pos?

                    if (matched is LongRange match)
                    {
                        // flush any literal before the match
                        if (!AppendToPendingCopy(match))
                        {
                            WritePendingCopy();
                            _pendingCopyRange = match;
                        }
                        // consumed = block end
                    }
                    else // not matched
                    {
                        WritePendingCopy();
                        // consumed = 0
                        // need to set examined
                    }
                }
                _reader.AdvanceTo(consumed, examined);

                if (_writtenAfterFlush >= _flushThreshhold)
                {
                    flushResult = await _writer.FlushAsync(ct).ConfigureAwait(false);
                    _writtenAfterFlush = 0;
                }
            }
        }
        private bool AppendToPendingCopy(LongRange range)
        {
            if (_pendingCopyRange.Length == 0)
            {
                _pendingCopyRange = range;
                return true;
            }
            checked
            {
                if (_pendingCopyRange.Start + _pendingCopyRange.Length == range.Start)
                {
                    _pendingCopyRange = new LongRange(
                        start: _pendingCopyRange.Start,
                        length: _pendingCopyRange.Length + range.Length);
                    return true;
                }
            }
            return false;
        }

        private void WritePendingCopy()
        {
            if (_pendingCopyRange.Length == 0)
            {
                return;
            }
            _writtenAfterFlush += _writer.Write(new CopyCommand(_pendingCopyRange));
            _pendingCopyRange = default;
        }
    }
}
