using System.Buffers;

namespace Rsync.Delta.Models
{
    internal interface IReadable<T> where T : struct
    {
        int MaxSize { get; }
        T? ReadFrom(ref ReadOnlySequence<byte> data);
    }

    internal interface IReadable<T, Options> where T : struct
    {
        int MaxSize(Options options);
        T? ReadFrom(
            ref ReadOnlySequence<byte> data,
            Options options);
    }
}