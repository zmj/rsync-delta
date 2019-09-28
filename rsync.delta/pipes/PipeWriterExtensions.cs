using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;

namespace Rsync.Delta.Pipes
{
    internal static class PipeWriterExtensions
    {
        public static int Write<T>(
            this PipeWriter writer,
            T writable)
            where T : IWritable
        {
            int size = writable.Size;
            var buffer = writer.GetSpan(size);
            writable.WriteTo(buffer);
            writer.Advance(size);
            return size;
        }

        public static int Write<T, Options>(
            this PipeWriter writer,
            T writable,
            Options options)
            where T : IWritable<Options>
        {
            int size = writable.Size(options);
            var buffer = writer.GetSpan(size);
            writable.WriteTo(buffer, options);
            writer.Advance(size);
            return size;
        }

        public static async ValueTask CopyFrom(
            this PipeWriter writer,
            PipeReader reader,
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

        public static async ValueTask CopyFrom(
            this PipeWriter writer,
            Stream readStream,
            long count,
            CancellationToken ct)
        {
            var reader = PipeReader.Create(readStream); // don't do this
            await writer.CopyFrom(reader, count, ct);
        }
    }
}