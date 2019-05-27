using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal readonly struct Copier
    {
        private readonly Stream _stream;
        private readonly PipeWriter _writer;
        private readonly StreamPipeReaderOptions _fileReadOptions;

        public Copier(
            Stream stream, 
            PipeWriter writer, 
            StreamPipeReaderOptions fileReadOptions)
        {
            _stream = stream;
            _writer = writer;
            _fileReadOptions = fileReadOptions;
        }

        public async ValueTask WriteCopy(LongRange range, CancellationToken ct)
        {
            if ((ulong)_stream.Position != range.Start)
            {
                // is there any benefit to choosing seekorigin based on position?
                _stream.Seek((long)range.Start, SeekOrigin.Begin); // fix this cast
            }
            long count = (long)range.Length;
            var reader = PipeReader.Create(_stream, _fileReadOptions);
            await reader.CopyTo(_writer, count, ct);
        }
    }
}