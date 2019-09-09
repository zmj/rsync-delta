using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct LiteralCommand : IWritable, IReadable<LiteralCommand>
    {
        private const byte _baseCommand = 0x41;

        private readonly CommandArg _length;

        public LiteralCommand(ulong length) => _length = new CommandArg(length);

        private LiteralCommand(
            ref ReadOnlySequence<byte> buffer,
            CommandModifier lengthModifier) =>
            _length = new CommandArg(ref buffer, lengthModifier);

        public ulong LiteralLength => _length.Value;

        public int Size => 1 + _length.Size;

        public int MaxSize => 1 + CommandArg.MaxSize;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            byte command = (byte)(_baseCommand + (byte)_length.Modifier);
            buffer[0] = command;
            _length.WriteTo(buffer.Slice(1));
        }

        public LiteralCommand? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            byte command = data.FirstByte();
            const byte maxCommand = _baseCommand + (byte)CommandModifier.EightBytes;
            if (command < _baseCommand || command > maxCommand)
            {
                return null;
            }
            data = data.Slice(1);
            return new LiteralCommand(
                ref data,
                (CommandModifier)(command - _baseCommand));
        }
    }
}