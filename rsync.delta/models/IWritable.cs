using System;

namespace Rsync.Delta.Models
{
    internal interface IWritable
    {
        int Size { get; }
        void WriteTo(Span<byte> buffer);
    }

    internal interface IWritable<Options>
    {
        int Size(Options options);
        void WriteTo(Span<byte> buffer, Options options);
    }
}