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

        public Copier(Stream stream, PipeWriter writer)
        {
            _stream = stream;
            _writer = writer;
        }

        public async ValueTask WriteCopy(LongRange range, CancellationToken ct)
        {
            Console.WriteLine($"copy: {range.Start} {range.Length}");
            if ((ulong)_stream.Position != range.Start)
            {
                // is there any benefit to choosing seekorigin based on position?
                _stream.Seek((long)range.Start, SeekOrigin.Begin); // fix this cast
            }
            long count = (long)range.Length;
            while (count > 0)
            {
                var buffer = _writer.GetMemory();
                if (buffer.Length > count)
                {
                    buffer = buffer.Slice(0, (int)count);
                }
                int read = await _stream.ReadAsync(buffer, ct);
                if (read == 0)
                {
                    throw new Exception("unexpected end of stream");
                }
                _writer.Advance(read);
                await _writer.FlushAsync(ct); // handle flushresult
                count -= read;
            }
        }
    }
}