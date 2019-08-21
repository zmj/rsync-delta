using System;

namespace Rsync.Delta.Models
{
    internal readonly struct EndCommand : IWritable
    {
        public int Size => 1;

        public void WriteTo(Span<byte> buffer) => buffer[0] = 0;
    }
}