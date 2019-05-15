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

        public static async ValueTask<ReadResult> Buffer2(
            this PipeReader reader,
            long count,
            CancellationToken ct)
        {
            // todo: make a non-async path?
            while (true)
            {
                ReadResult result;
                if (!reader.TryRead(out result))
                {
                    result = await reader.ReadAsync(ct);
                }
                if (result.Buffer.Length >= count)
                {
                    result = new ReadResult(
                        result.Buffer.Slice(0, count),
                        isCanceled: result.IsCanceled,
                        isCompleted: result.IsCompleted);
                    return result;
                }
                else if (result.IsCompleted || result.IsCanceled)
                {
                    return result;
                }
                reader.AdvanceTo(
                    consumed: result.Buffer.Start,
                    examined: result.Buffer.End);
            }
        }
    }
}