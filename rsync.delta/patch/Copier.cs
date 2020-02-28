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

        public Copier(
            Stream stream,
            PipeWriter writer)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("copy source must be seekable");
            }
            _stream = stream;
            _writer = writer;
        }

        public async ValueTask<FlushResult> WriteCopy(LongRange range, CancellationToken ct)
        {
            SeekTo(range.Start);
            return await _writer.CopyFrom(_stream, range.Length, ct).ConfigureAwait(false);
        }

        private void SeekTo(long position)
        {
            if (_stream.Position == position)
            {
                return;
            }
            var offset = position - _stream.Position;
            _stream.Seek(offset, SeekOrigin.Current);
        }
    }
}
