using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct LiteralCommand : IWritable
    {
        private const byte _baseCommand = 0x41;

        private readonly CommandArg _length;

        public LiteralCommand(ulong length) => _length = new CommandArg(length);

        private LiteralCommand(
            ReadOnlySequence<byte> buffer,
            CommandModifier lengthModifier) =>
            _length = new CommandArg(ref buffer, lengthModifier);

        public ulong LiteralLength => _length.Value;

        public int Size => 1 + _length.Size;

        public const int MaxSize = 1 + CommandArg.MaxSize;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            byte command = (byte)(_baseCommand + (byte)_length.Modifier);
            buffer[0] = command;
            _length.WriteTo(buffer.Slice(1));
        }

        public static bool TryParse(
            ReadOnlySequence<byte> buffer,
            out LiteralCommand literal)
        {
            byte command = buffer.ReadByte();
            if (command < _baseCommand ||
                command > (_baseCommand + (byte)CommandModifier.EightBytes))
            {
                literal = default;
                return false;
            }
            literal = new LiteralCommand(
                buffer,
                (CommandModifier)(command - _baseCommand));
            return true;
        }
    }
}