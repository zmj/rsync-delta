using System;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct CopyCommand
    {
        private const byte _baseCommand = 0x45;

        private readonly CommandArg _start;
        private readonly CommandArg _length;

        public CopyCommand(LongRange range)
        {
            _start = new CommandArg(range.Start);
            _length = new CommandArg(range.Length);
        }

        public int Size => 1 + _start.Size + _length.Size;

        public void WriteTo(Span<byte> buffer)
        {
            byte command = (byte)(_baseCommand +
                (4 * (byte)_start.Modifier) +
                (byte)_length.Modifier);
            buffer[0] = command;
            _start.WriteTo(buffer.Slice(1));
            _length.WriteTo(buffer.Slice(1 + _start.Size));
        }
    }
}