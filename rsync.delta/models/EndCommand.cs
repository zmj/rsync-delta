using System;
using System.Buffers;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct EndCommand : IWritable, IReadable<EndCommand>
    {
        public int Size => 1;
        public int MaxSize => Size;
        public int MinSize => Size;

        public void WriteTo(Span<byte> buffer) => buffer[0] = 0;

        public EndCommand? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            if (data.FirstByte() != 0)
            {
                return null;
            }
            data = data.Slice(Size);
            return new EndCommand();
        }

        public override string ToString() => "END";
    }
}
