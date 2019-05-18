using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal static class PipeExtensions
    {
        public static ValueTask<ReadResult> Buffer(
            this PipeReader reader,
            long count,
            CancellationToken ct)
        {
            if (reader.TryRead(out var readResult) &&
                readResult.Buffered(count))
            {
                return new ValueTask<ReadResult>(readResult);
            }
            return BufferAsync(reader, count, ct);
        }

        private static async ValueTask<ReadResult> BufferAsync(
            PipeReader reader,
            long count,
            CancellationToken ct)
        {
            while (true)
            {
                var readResult = await reader.ReadAsync(ct);
                if (readResult.Buffered(count))
                {
                    return readResult;
                }
                reader.AdvanceTo(
                    consumed: readResult.Buffer.Start,
                    examined: readResult.Buffer.End);
            }
        }

        private static bool Buffered(this ref ReadResult result, long count)
        {
            if (result.Buffer.Length >= count)
            {
                result = new ReadResult(
                    result.Buffer.Slice(0, count),
                    isCanceled: result.IsCanceled,
                    isCompleted: result.IsCompleted);
                return true;
            }
            return result.IsCompleted || result.IsCanceled;
        }

        public static async ValueTask CopyTo(
            this PipeReader reader,
            PipeWriter writer,
            long count,
            CancellationToken ct)
        {
            while (count > 0)
            {
                var readResult = await reader.ReadAsync(ct); // handle result
                var readBuffer = readResult.Buffer.First;
                var writeBuffer = writer.GetMemory(readBuffer.Length);
                readBuffer.CopyTo(writeBuffer);
                writer.Advance(readBuffer.Length);
                reader.AdvanceTo(readResult.Buffer.GetPosition(readBuffer.Length));
                await writer.FlushAsync(ct); // handle result
                count -= readBuffer.Length;
            }
        }
    }
}