using System;
using System.Buffers;

namespace Rsync.Delta.Models
{
    internal interface IReadable<T> where T : struct
    {
        int MaxSize { get; }
        int MinSize { get; }
        T? ReadFrom(ref ReadOnlySequence<byte> data);
    }

    internal interface IReadable2<T> where T : struct
    {
        int Size { get; }
        int MaxSize { get; }
        int MinSize { get; }
        T? TryReadFrom(ReadOnlySpan<byte> span);
    }

    internal interface IReadable2<T, Options> where T : struct
    {
        int Size(Options options);
        int MaxSize(Options options);
        int MinSize(Options options);
        T? TryReadFrom(ReadOnlySpan<byte> span, Options options);
    }

    internal interface IReadable<T, Options> where T : struct
    {
        int MaxSize(Options options);
        int MinSize(Options options);
        T? ReadFrom(
            ref ReadOnlySequence<byte> data,
            Options options);
    }
}
