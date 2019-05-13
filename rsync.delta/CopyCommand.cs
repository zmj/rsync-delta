using System;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct CopyCommand
    {
        public readonly LongRange Range;

        public CopyCommand(LongRange range) => Range = range;

        public int Size => 12;

        public void WriteTo(Span<byte> buffer)
        {
            // validate size
            
            uint magic = 0x45; // copy, 1 byte arg1, 1 byte arg2
            BinaryPrimitives.WriteUInt32BigEndian(buffer, magic);
            buffer = buffer.Slice(4);

            uint start = (uint)Range.Start;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, start);
            buffer = buffer.Slice(4);

            uint length = (uint)Range.Length;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, length);
        }

        // static tryread?
    }
}