using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;

namespace Rsync.Delta.Patch
{
    internal readonly struct Copier
    {
        private readonly Stream _stream;
        private readonly PipeWriter _writer;
        private readonly StreamPipeReaderOptions _readerOptions;

        public Copier(
            Stream stream, 
            PipeWriter writer, 
            StreamPipeReaderOptions readerOptions)
        {
            Debug.Assert(stream.CanSeek);
            _stream = stream;
            _writer = writer;
            _readerOptions = readerOptions;
        }

        public async ValueTask WriteCopy(LongRange range, CancellationToken ct)
        {
            if ((ulong)_stream.Position != range.Start)
            {
                // is there any benefit to choosing seekorigin based on position?
                _stream.Seek((long)range.Start, SeekOrigin.Begin); // fix this cast
            }
            long count = (long)range.Length;
            var reader = PipeReader.Create(_stream, _readerOptions); // don't do this
            await reader.CopyTo(_writer, count, ct);
        }
    }

    internal static class CopyExtensions
    {
        public static async ValueTask CopyTo(
            this PipeReader reader,
            PipeWriter writer,
            long count,
            CancellationToken ct)
        {
            int writtenSinceFlush = 0;
            while (count > 0)
            {
                var readResult = await reader.ReadAsync(ct); // handle result
                var readBuffer = readResult.Buffer.First;
                if (readBuffer.Length > count)
                {
                    readBuffer = readBuffer.Slice(0, (int)count);
                }
                var writeBuffer = writer.GetMemory(readBuffer.Length);
                readBuffer.CopyTo(writeBuffer);
                writer.Advance(readBuffer.Length);
                reader.AdvanceTo(readResult.Buffer.GetPosition(readBuffer.Length));
                writtenSinceFlush += readBuffer.Length;
                if (writtenSinceFlush > 1 << 12)
                {
                    await writer.FlushAsync(ct); // handle result
                    writtenSinceFlush = 0;
                }
                count -= readBuffer.Length;
            }
            if (writtenSinceFlush > 0)
            {
                await writer.FlushAsync(ct);
            }
        }
    }
}