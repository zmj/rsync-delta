using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;

namespace Rsync.Delta.Pipes
{
    internal static class PipeReaderExtensions
    {
        public static async ValueTask<T?> Read<T>(
            this PipeReader reader,
            CancellationToken ct)
            where T : struct, IReadable<T>
        {
            while (true)
            {
                var readResult = await reader.ReadAsync(ct).ConfigureAwait(false);
                if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
                {
                    return null;
                }
                var opStatus = readResult.Buffer.Read(out T value);
                if (opStatus == OperationStatus.Done)
                {
                    var consumed = readResult.Buffer.GetPosition(value.Size);
                    reader.AdvanceTo(consumed);
                    return value;
                }
                else if (opStatus == OperationStatus.NeedMoreData)
                {
                    if (readResult.IsCompleted)
                    {
                        throw new FormatException($"expected a {typeof(T).Name}; got EOF");
                    }
                    reader.AdvanceTo(
                        consumed: readResult.Buffer.Start,
                        examined: readResult.Buffer.End);
                }
                else if (opStatus == OperationStatus.InvalidData)
                {
                    throw new FormatException($"expected a {typeof(T).Name}");
                }
                else
                {
                    throw new ArgumentException($"unexpected {nameof(OperationStatus)}.{opStatus}");
                }
            }
        }

        public static async ValueTask<T?> Read<T, Options>(
            this PipeReader reader,
            Options options,
            CancellationToken ct)
            where T : struct, IReadable<T, Options>
        {
            while (true)
            {
                var readResult = await reader.ReadAsync(ct).ConfigureAwait(false);
                if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
                {
                    return null;
                }
                var opStatus = readResult.Buffer.Read(options, out T value);
                if (opStatus == OperationStatus.Done)
                {
                    var consumed = readResult.Buffer.GetPosition(value.Size(options));
                    reader.AdvanceTo(consumed);
                    return value;
                }
                else if (opStatus == OperationStatus.NeedMoreData)
                {
                    if (readResult.IsCompleted)
                    {
                        throw new FormatException($"expected a {typeof(T).Name}; got EOF");
                    }
                    reader.AdvanceTo(
                        consumed: readResult.Buffer.Start,
                        examined: readResult.Buffer.End);
                }
                else if (opStatus == OperationStatus.InvalidData)
                {
                    throw new FormatException($"expected a {typeof(T).Name}");
                }
                else
                {
                    throw new ArgumentException($"unexpected {nameof(OperationStatus)}.{opStatus}");
                }
            }
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
                return result.IsCompleted;
            }
            result = new ReadResult(
                result.Buffer.Slice(0, count),
                isCanceled: result.IsCanceled,
                isCompleted: result.IsCompleted);
            return true;
        }
    }
}
