using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Pipes;

namespace Rsync.Delta
{
    internal class DeltaWriter
    {
        private readonly BlockMatcher _blocks;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private const uint _flushThreshhold = 1 << 13;

        private LongRange _pendingCopyRange;
        private ulong _pendingLiteralLength;
        private uint _writtenAfterFlush;

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
                WriteHeader();
                await WriteCommands(ct);
                WriteEndCommand();
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

        private void WriteHeader()
        {
            var buffer = _writer.GetSpan(DeltaHeader.Size);
            new DeltaHeader().WriteTo(buffer);
            _writer.Advance(DeltaHeader.Size);
            _writtenAfterFlush += DeltaHeader.Size;
        }

        private async ValueTask WriteCommands(CancellationToken ct)
        {
            while (true)
            {
                var buffer = await BufferBlock(ct);
                if (buffer.CurrentBlock.IsEmpty)
                {
                    // eof: flush anything pending
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
                    // todo: cap the read buffer
                    // const size_t max_miss = 32768;
                    // if > max flush instead
                    _pendingLiteralLength++;
                    _reader.AdvanceTo(
                        consumed: buffer.PendingLiteral.Start,
                        examined: buffer.CurrentBlock.End);
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
            if (_pendingCopyRange.Start + _pendingCopyRange.Length == range.Start) // check overflow
            {
                _pendingCopyRange = new LongRange(
                    start: _pendingCopyRange.Start, 
                    length: _pendingCopyRange.Length + range.Length);
                return true;
            }
            return false;
        }

        private void WritePendingCopy()
        {
            if (_pendingCopyRange.Length == 0)
            {
                return;
            }
            var command = new CopyCommand(_pendingCopyRange);
            command.WriteTo(_writer.GetSpan(command.Size));
            _writer.Advance(command.Size);
            _writtenAfterFlush += (uint)command.Size; // fix cast
            _pendingCopyRange = default;
        }

        private async ValueTask FlushPendingLiteral(
            ReadOnlySequence<byte> pendingLiteral,
            CancellationToken ct)
        {
            Debug.Assert(_pendingLiteralLength == (ulong)pendingLiteral.Length);
            if (pendingLiteral.IsEmpty)
            {
                return;
            }
            var command = new LiteralCommand((ulong)pendingLiteral.Length);
            command.WriteTo(_writer.GetSpan(command.Size));
            _writer.Advance(command.Size);
            
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
            ulong len = (uint)_blocks.BlockLength + _pendingLiteralLength;
            var readResult = await _reader.Buffer((long)len, ct); // fix this cast
            var pendingLiteral = readResult.Buffer.Slice(0, (int)_pendingLiteralLength); // fix
            var currentBlock = readResult.Buffer.Slice((int)_pendingLiteralLength); // fix
            return new BufferedBlock(pendingLiteral, currentBlock);
        }
        
        private void WriteEndCommand()
        {
            var buffer = _writer.GetSpan(1);
            buffer[0] = 0;
            _writer.Advance(1);
        }
    }
}