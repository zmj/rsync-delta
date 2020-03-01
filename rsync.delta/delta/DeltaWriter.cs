using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal readonly struct DeltaWriter
        <TRollingHashAlgorithm, TStrongHashAlgorithm>
        where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
        where TStrongHashAlgorithm : IStrongHashAlgorithm
    {
        private readonly BlockMatcher<TRollingHashAlgorithm, TStrongHashAlgorithm> _matcher;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private readonly int _blockLength;

        private const int _flushThreshhold = 1 << 12;
        private const int _maxLiteralLength = 1 << 15;

        public DeltaWriter(
            SignatureOptions options,
            BlockMatcher<TRollingHashAlgorithm, TStrongHashAlgorithm> matcher,
            PipeReader reader,
            PipeWriter writer)
        {
            _blockLength = options.BlockLength;
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
                    WriteCopyCommand(pendingCopy);
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
            in ReadOnlySequence<byte> buffered,
            bool isFinalBlock,
            ref long pendingLiteral,
            ref LongRange pendingCopy)
        {
            var bufferedLiteral = buffered.Slice(0, pendingLiteral);
            var toMatch = buffered.Slice(pendingLiteral);
            int written = 0;
            while (_matcher.TryMatchBlock(
                    toMatch, isFinalBlock,
                    out var matchStart, out var match))
            {
                var literal = toMatch.Slice(0, matchStart);
                if (!bufferedLiteral.IsEmpty)
                {
                    var len = bufferedLiteral.Length + matchStart;
                    literal = buffered.Slice(0, len);
                    bufferedLiteral = ReadOnlySequence<byte>.Empty;
                }

                if (!literal.IsEmpty)
                {
                    written += WriteCopyCommand(pendingCopy);
                    pendingCopy = default;
                    written += WriteLiteral(literal);
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

                toMatch = toMatch.Slice(matchStart + match.Length);
            }

            var remainder = toMatch;
            if (!bufferedLiteral.IsEmpty)
            {
                var len = bufferedLiteral.Length + remainder.Length;
                remainder = buffered.Slice(0, len);
                bufferedLiteral = ReadOnlySequence<byte>.Empty;
            }

            if (isFinalBlock)
            {
                written += WriteCopyCommand(pendingCopy);
                pendingCopy = default;
                written += WriteLiteral(remainder);
                remainder = remainder.Slice(remainder.End);
            }
            
            var incompleteBlock = Math.Min(_blockLength - 1, remainder.Length);
            pendingLiteral = remainder.Length - incompleteBlock;
            if (pendingLiteral >= _maxLiteralLength)
            {
                written += WriteCopyCommand(pendingCopy);
                pendingCopy = default;
                var len = pendingLiteral;
                len -= pendingLiteral = pendingLiteral % _maxLiteralLength;
                written += WriteLiteral(remainder.Slice(0, len));
                remainder = remainder.Slice(len);
            }

            _reader.AdvanceTo(
                consumed: remainder.Start,
                examined: buffered.End);
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

        private int WriteLiteralCommand(in ReadOnlySequence<byte> literal)
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
