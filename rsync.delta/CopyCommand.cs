using System;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct CopyCommand
    {
        public readonly LongRange Range;

        public CopyCommand(LongRange range) => Range = range;

        public int Size => 3;

        public void WriteTo(Span<byte> buffer)
        {
            // validate size

            buffer[0] = 0x45; // copy, 1 byte arg1, 1 byte arg2
            buffer[1] = (byte)Range.Start;
            buffer[2] = (byte)Range.Length;
            Console.WriteLine($"{BitConverter.ToString(buffer.Slice(0,3).ToArray())} s:{Range.Start} l:{Range.Length}");
        }

        // static tryread?
    }
}