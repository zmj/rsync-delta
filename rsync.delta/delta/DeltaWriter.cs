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
    internal readonly struct DeltaWriter
    {
        private readonly BlockMatcher _matcher;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private const int _flushThreshhold = 1 << 12;
        private const int _maxLiteralLength = 1 << 15;

        public DeltaWriter(
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
                await WriteCommandsAsync(ct).ConfigureAwait(false);
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

        private async ValueTask WriteCommandsAsync(CancellationToken ct)
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
                    Debug.Assert(pendingLiteral == 0);
                    Debug.Assert(pendingCopy.Length == 0);
                    _writer.Write(new EndCommand());
                    return;
                }

                writtenAfterFlush += WriteCommands(
                    readResult.Buffer,
                    isFinalBlock: readResult.IsCompleted,
                    ref pendingLiteral,
                    ref pendingCopy);

                if (writtenAfterFlush >= _flushThreshhold)
                {
                    flushResult = await _writer.FlushAsync(ct).ConfigureAwait(false);
                    writtenAfterFlush = 0;
                }
            }
        }

        private int WriteCommands(
            in ReadOnlySequence<byte> sequence,
            bool isFinalBlock,
            ref long pendingLiteral,
            ref LongRange pendingCopy)
        {
            long consumed = 0;
            int written = 0;
            while (_matcher.TryMatchBlock(
                    sequence.Slice(pendingLiteral + consumed),
                    isFinalBlock,
                    out long matchStart,
                    out LongRange match))
            {
                var literalLength = pendingLiteral + matchStart;
                if (literalLength > 0)
                {
                    written += WriteCopyCommand(pendingCopy);
                    pendingCopy = default;
                    var literal = sequence.Slice(consumed, literalLength);
                    written += WriteLiteral(literal);
                    pendingLiteral = 0;
                }

                if (pendingCopy.TryAppend(match, out var appended))
                {
                    pendingCopy = appended;
                }
                else
                {
                    written += WriteCopyCommand(pendingCopy);
                    pendingCopy = match;
                }

                consumed += literalLength + match.Length;
            }

            var remainder = sequence.Slice(consumed);
            if (isFinalBlock)
            {
                written += WriteCopyCommand(pendingCopy);
                pendingCopy = default;
                written += WriteLiteral(remainder);
                consumed += remainder.Length;
            }
            else if (remainder.Length > _maxLiteralLength)
            {
                throw new NotImplementedException();
            }

            pendingLiteral = sequence.Length - consumed;
            _reader.AdvanceTo(
                consumed: sequence.GetPosition(consumed),
                examined: sequence.End);
            return written;
        }

        private int WriteCopyCommand(LongRange range)
        {
            if (range.Length == 0)
            {
                return 0;
            }
            return _writer.Write(new CopyCommand(range));
        }

        private int WriteLiteral(in ReadOnlySequence<byte> sequence)
        {
            if (sequence.IsEmpty)
            {
                return 0;
            }
            var toWrite = sequence;
            int written = 0;
            do
            {
                var len = Math.Min(toWrite.Length, _maxLiteralLength);
                written += WriteLiteralCommand(toWrite.Slice(0, len));
                toWrite = toWrite.Slice(len);
            } while (!toWrite.IsEmpty);
            return written;
        }

        private int WriteLiteralCommand(ReadOnlySequence<byte> literal)
        {
            Debug.Assert(!literal.IsEmpty);
            Debug.Assert(literal.Length <= _maxLiteralLength);
            int written = _writer.Write(new LiteralCommand((ulong)literal.Length));
            foreach (var readMemory in literal)
            {
                var readSpan = readMemory.Span;
                while (!readSpan.IsEmpty)
                {
                    var writeSpan = _writer.GetSpan();
                    var len = Math.Min(readSpan.Length, writeSpan.Length);
                    readSpan.Slice(0, len).CopyTo(writeSpan);
                    _writer.Advance(len);
                    written += len;
                    readSpan = readSpan.Slice(len);
                }
            }
            return written;
        }
    }
}
