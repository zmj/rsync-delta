using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;

namespace Rsync.Delta.Pipes
{
    internal static class PipeReaderExtensions
    {
        public static async ValueTask<T?> Read2<T>(
            this PipeReader reader,
            CancellationToken ct)
            where T : struct, IReadable2<T>
        {
            var maxSize = default(T).MaxSize;
            var readResult = await reader.Buffer(maxSize, ct).ConfigureAwait(false);
            if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
            {
                return null;
            }
            T? result = readResult.Buffer.TryRead<T>();
            if (!result.HasValue)
            {
                throw new FormatException($"expected a {typeof(T).Name}");
            }
            var consumed = readResult.Buffer.GetPosition(result.Value.Size);
            reader.AdvanceTo(consumed);
            return result.Value;
        }

        public static async ValueTask<T?> Read2<T, Options>(
            this PipeReader reader,
            Options options,
            CancellationToken ct)
            where T : struct, IReadable2<T, Options>
        {
            var maxSize = default(T).MaxSize(options);
            var readResult = await reader.Buffer(maxSize, ct).ConfigureAwait(false);
            if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
            {
                return null;
            }
            T? result = readResult.Buffer.TryRead<T, Options>(options);
            if (!result.HasValue)
            {
                throw new FormatException($"expected a {typeof(T).Name}");
            }
            var consumed = readResult.Buffer.GetPosition(result.Value.Size(options));
            reader.AdvanceTo(consumed);
            return result.Value;
        }

        public static async ValueTask<T?> Read<T>(
            this PipeReader reader,
            CancellationToken ct)
            where T : struct, IReadable<T>
        {
            T t = default;
            var readResult = await reader.Buffer(t.MaxSize, ct).ConfigureAwait(false);
            var buffer = readResult.Buffer;
            T? result = buffer.Length >= t.MinSize ?
                t.ReadFrom(ref buffer) : null;
            reader.AdvanceTo(buffer.Start);
            return result;
        }

        public static async ValueTask<T?> Read<T, Options>(
            this PipeReader reader,
            Options options,
            CancellationToken ct)
            where T : struct, IReadable<T, Options>
        {
            T t = default;
            var readResult = await reader.Buffer(t.MaxSize(options), ct).ConfigureAwait(false);
            var buffer = readResult.Buffer;
            T? result = buffer.Length >= t.MinSize(options) ?
                t.ReadFrom(ref buffer, options) : null;
            reader.AdvanceTo(buffer.Start);
            return result;
        }

        public static ValueTask<ReadResult> Buffer(
            this PipeReader reader,
            int count,
            CancellationToken ct)
        {
            Debug.Assert(count > 0);
            if (reader.TryRead(out var readResult))
            {
                if (readResult.Buffered(count))
                {
                    return new ValueTask<ReadResult>(readResult);
                }
                reader.AdvanceTo(
                    consumed: readResult.Buffer.Start,
                    examined: readResult.Buffer.End);
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
                var readResult = await reader.ReadAsync(ct).ConfigureAwait(false);
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
