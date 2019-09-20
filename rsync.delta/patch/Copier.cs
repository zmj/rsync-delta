using System;
using System.Diagnostics;
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
            await _writer.CopyFrom(_stream, count, ct);
        }
    }
}