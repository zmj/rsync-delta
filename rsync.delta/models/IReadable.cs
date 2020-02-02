using System;
using System.Buffers;

namespace Rsync.Delta.Models
{
    internal interface IReadable<T> where T : struct
    {
        int Size { get; }
        int MaxSize { get; }
        int MinSize { get; }
        OperationStatus ReadFrom(ReadOnlySpan<byte> span, out T value);
    }

    internal interface IReadable<T, Options> where T : struct
    {
        int Size(Options options);
        int MaxSize(Options options);
        int MinSize(Options options);
        OperationStatus ReadFrom(ReadOnlySpan<byte> span, Options options, out T value);
    }
}
