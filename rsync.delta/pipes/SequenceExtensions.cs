using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Rsync.Delta.Models;

namespace Rsync.Delta.Pipes
{
    internal static class SequenceExtensions
    {
        public static OperationStatus Read<T>(
            this in ReadOnlySequence<byte> sequence,
            out T value)
            where T : struct, IReadable<T>
        {
            T t = default;
            long seqLen = sequence.Length;
            if (seqLen < t.MinSize)
            {
                value = default;
                return OperationStatus.NeedMoreData;
            }
            int maxSize = t.MaxSize;
            int len = seqLen > maxSize ? maxSize : (int)seqLen;
            return sequence.TryGetSpan(len, out var span) ?
                t.ReadFrom(span, out value) :
                t.ReadFrom(sequence.CopyTo(stackalloc byte[len]), out value);
        }

        public static OperationStatus Read<T, Options>(
            this in ReadOnlySequence<byte> sequence,
            Options options,
            out T value)
            where T : struct, IReadable<T, Options>
        {
            T t = default;
            long seqLen = sequence.Length;
            if (seqLen < t.MinSize(options))
            {
                value = default;
                return OperationStatus.NeedMoreData;
            }
            int maxSize = t.MaxSize(options);
            int len = seqLen > maxSize ? maxSize : (int)seqLen;
            return sequence.TryGetSpan(len, out var span) ?
                t.ReadFrom(span, options, out value) :
                t.ReadFrom(
                    sequence.CopyTo(stackalloc byte[len]),
                    options,
                    out value);
        }

        public static bool TryGetSpan(
            this in ReadOnlySequence<byte> sequence,
            int spanLength,
            out ReadOnlySpan<byte> span)
        {
            Debug.Assert(sequence.Length >= spanLength);
            var firstSpan = sequence.FirstSpan();
            if (firstSpan.Length >= spanLength)
            {
                span = firstSpan.Slice(0, spanLength);
                return true;
            }
            span = default;
            return false;
        }

        public static ReadOnlySpan<byte> CopyTo(
            this in ReadOnlySequence<byte> sequence,
            Span<byte> span)
        {
            Debug.Assert(sequence.Length >= span.Length);
            BuffersExtensions.CopyTo(sequence.Slice(0, span.Length), span);
            return span;
        }

        public static byte LastByte(this in ReadOnlySequence<byte> sequence)
        {
            Debug.Assert(sequence.Length > 0);
            if (sequence.IsSingleSegment)
            {
                var span = sequence.FirstSpan();
                return span[span.Length - 1];
            }
            byte last = default;
            foreach (var memory in sequence)
            {
                var span = memory.Span;
                if (!span.IsEmpty)
                {
                    last = span[span.Length - 1];
                }
            }
            return last;
        }

        public static byte FirstByte(this in ReadOnlySequence<byte> sequence)
        {
            Debug.Assert(sequence.Length > 0);
            var firstSpan = sequence.FirstSpan();
            if (!firstSpan.IsEmpty)
            {
                return firstSpan[0];
            }
            foreach (var memory in sequence)
            {
                var span = memory.Span;
                if (!span.IsEmpty)
                {
                    return span[0];
                }
            }
            Debug.Assert(false, "unreachable");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> FirstSpan(
            this in ReadOnlySequence<byte> sequence)
        {
#if !NETSTANDARD2_0
            return sequence.FirstSpan;
#else
            return sequence.First.Span;
#endif
        }
    }
}
