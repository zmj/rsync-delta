using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

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
            if (!stream.CanSeek)
            {
                throw new ArgumentException("copy source must be seekable");
            }
            _stream = stream;
            _writer = writer;
            _readerOptions = readerOptions;
        }

        public async ValueTask<FlushResult> WriteCopy(LongRange range, CancellationToken ct)
        {
            SeekTo(range.Start);
            long count = checked((long)range.Length);
            return await _writer.CopyFrom(_stream, count, ct);
        }

        private void SeekTo(ulong position)
        {
            ulong currentPosition = checked((ulong)_stream.Position);
            if (currentPosition == position)
            {
                return;
            }
            long offset = (long)(position - currentPosition);
            _stream.Seek(offset, SeekOrigin.Current);
        }
    }
}
