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
                    await reader.ReadAsync(ct);
                }
                Console.WriteLine($"p: {result.Buffer.Length} {result.IsCompleted} ({count})");

                if (result.IsCompleted ||
                    result.IsCanceled ||
                    result.Buffer.Length >= count)
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