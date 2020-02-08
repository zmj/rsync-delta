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
                OperationStatus opStatus;
                SequencePosition consumed = readResult.Buffer.Start;
                do
                {
                    opStatus = _matcher.MatchBlock(
                        readResult.Buffer,
                        readResult.IsCompleted,
                        out LongRange? matched);
                    // todo - how is consumed updated? int or pos?
                } while (opStatus == OperationStatus.Done);
                Debug.Assert(opStatus == OperationStatus.NeedMoreData);
                _reader.AdvanceTo(consumed, readResult.Buffer.End);
            }
        }
    }
}
