using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal class DeltaWriter
    {
        private readonly BlockMatcher _blocks;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private const uint _flushThreshhold = 1 << 12;

        private LongRange _pendingCopyRange;
        private int _pendingLiteralLength;
        private int _writtenAfterFlush;

        public DeltaWriter(BlockMatcher blocks, PipeReader reader, PipeWriter writer)
        {
            _blocks = blocks;
            _reader = reader;
            _writer = writer;
        }

        public async ValueTask Write(CancellationToken ct)
        {
            try
            {
                _writtenAfterFlush += _writer.Write(new DeltaHeader());
                await WriteCommands(ct);
                _writer.Write(new EndCommand());
                await _writer.FlushAsync(ct); // handle flushresult
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
            while (true)
            {
                var buffer = await BufferBlock(ct);
                if (buffer.CurrentBlock.IsEmpty)
                {
                    WritePendingCopy();
                    await FlushPendingLiteral(buffer.PendingLiteral, ct);
                    return;
                }

                LongRange? matched = _blocks.MatchBlock(buffer);
                if (matched.HasValue)
                {
                    await FlushPendingLiteral(buffer.PendingLiteral, ct);
                    if (!AppendToPendingCopy(matched.Value))
                    {
                        WritePendingCopy();
                        _pendingCopyRange = matched.Value;
                    }
                    _reader.AdvanceTo(consumed: buffer.CurrentBlock.End);
                }
                else // not matched
                {
                    WritePendingCopy();
                    if (_pendingLiteralLength == 1 << 15)
                    {
                        await FlushPendingLiteral(buffer.PendingLiteral, ct);
                    }
                    else
                    {
                        _pendingLiteralLength++;
                        _reader.AdvanceTo(
                            consumed: buffer.PendingLiteral.Start,
                            examined: buffer.CurrentBlock.End);
                    }
                }

                if (_writtenAfterFlush >= _flushThreshhold)
                {
                    await _writer.FlushAsync(ct); // handle flushresult
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

        private async ValueTask FlushPendingLiteral(
            ReadOnlySequence<byte> pendingLiteral,
            CancellationToken ct)
        {
            Debug.Assert(_pendingLiteralLength == pendingLiteral.Length);
            if (pendingLiteral.IsEmpty)
            {
                return;
            }
            _writer.Write(new LiteralCommand((ulong)pendingLiteral.Length));
            while (!pendingLiteral.IsEmpty)
            {
                var buffer = _writer.GetMemory((int)_flushThreshhold);
                int copyLen = buffer.Length > pendingLiteral.Length ?
                    (int)pendingLiteral.Length : buffer.Length;
                pendingLiteral.Slice(0, copyLen)
                    .CopyTo(buffer.Slice(0, copyLen).Span);
                pendingLiteral = pendingLiteral.Slice(copyLen);
                _writer.Advance(copyLen);
                await _writer.FlushAsync(ct); // handle flushresult
            }
            _pendingLiteralLength = 0;
            _writtenAfterFlush = 0;
            _reader.AdvanceTo(consumed: pendingLiteral.End);
        }

        private async ValueTask<BufferedBlock> BufferBlock(CancellationToken ct)
        {
            int len = _blocks.Options.BlockLength + _pendingLiteralLength;
            var readResult = await _reader.Buffer(len, ct);
            var pendingLiteral = readResult.Buffer.Slice(0, _pendingLiteralLength);
            var currentBlock = readResult.Buffer.Slice(_pendingLiteralLength);
            return new BufferedBlock(pendingLiteral, currentBlock);
        }
    }
}