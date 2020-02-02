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
        OperationStatus ReadFrom(ReadOnlySpan<byte> span, out T value);
    }

    internal interface IReadable2<T, Options> where T : struct
    {
        int Size(Options options);
        int MaxSize(Options options);
        int MinSize(Options options);
        OperationStatus ReadFrom(ReadOnlySpan<byte> span, Options options, out T value);
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
