using System.Buffers;

namespace Rsync.Delta.Models
{
    internal interface IReadable<T> where T : struct
    {
        int MaxSize { get; }
        int MinSize { get; }
        T? ReadFrom(ref ReadOnlySequence<byte> data);
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
