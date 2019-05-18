using System;

namespace Rsync.Delta
{
    internal readonly struct LiteralCommand
    {
        private const byte _baseCommand = 0x41;

        private readonly CommandArg _length;

        public LiteralCommand(ulong length) => _length = new CommandArg(length);

        public int Size => 1 + _length.Size;

        public void WriteTo(Span<byte> buffer)
        {
            byte command = (byte)(_baseCommand + (byte)_length.Modifier);
            buffer[0] = command;
            _length.WriteTo(buffer.Slice(1));
        }
    }
}