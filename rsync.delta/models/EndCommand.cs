using System;
using System.Buffers;

namespace Rsync.Delta.Models
{
    internal readonly struct EndCommand : IWritable, IReadable<EndCommand>
    {
        public int Size => 1;
        public int MaxSize => 1;

        public void WriteTo(Span<byte> buffer) => buffer[0] = 0;

        public EndCommand? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            if (data.First.Span[0] != 0)
            {
                return null;
            }
            data = data.Slice(Size);
            return new EndCommand();
        }
    }
}