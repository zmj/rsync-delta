using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta.Pipes
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
            if (result.Buffer.Length < count)
            {
                return result.IsCompleted || result.IsCanceled;
            }
            result = new ReadResult(
                result.Buffer.Slice(0, count),
                isCanceled: result.IsCanceled,
                isCompleted: result.IsCompleted);
            return true;
        }
    }
}