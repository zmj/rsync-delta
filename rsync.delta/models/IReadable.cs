using System.Buffers;

namespace Rsync.Delta.Models
{
    internal interface IReadable<T> where T : struct
    {
        int MaxSize { get; }
        T? ReadFrom(ReadOnlySequence<byte> data);
    }
}