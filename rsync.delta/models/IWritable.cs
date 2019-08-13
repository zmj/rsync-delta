using System;

namespace Rsync.Delta.Models
{
    internal interface IWritable
    {
        int Size { get; }
        void WriteTo(Span<byte> buffer);
    }
}