using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal static class PipeExtensions
    {
        public static async ValueTask<ReadResult> Buffer(
            this PipeReader reader,
            long count,
            CancellationToken ct)
        {
            while (true)
            {
                ReadResult result;
                if (!reader.TryRead(out result))
                {
                    result = await reader.ReadAsync(ct);
                }
                if (result.Buffer.Length >= count)
                {
                    return new ReadResult(
                        result.Buffer.Slice(0, count),
                        isCanceled: result.IsCanceled,
                        isCompleted: result.IsCompleted);
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