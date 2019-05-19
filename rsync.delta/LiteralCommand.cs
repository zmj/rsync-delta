using System;
using System.Buffers;
using System.Diagnostics;

namespace Rsync.Delta
{
    internal readonly struct LiteralCommand
    {
        private const byte _baseCommand = 0x41;

        private readonly CommandArg _length;

        public LiteralCommand(ulong length) => _length = new CommandArg(length);

        private LiteralCommand(
            SequenceReader<byte> reader, 
            CommandModifier lengthModifier) =>
            _length = new CommandArg(ref reader, lengthModifier);

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
            byte command = buffer.FirstSpan[0];
            if (command < _baseCommand ||
                command > (_baseCommand + (byte)CommandModifier.EightBytes))
            {
                literal = default;
                return false;
            }
            literal = new LiteralCommand(
                new SequenceReader<byte>(buffer.Slice(1)),
                (CommandModifier)(command - _baseCommand));
            return true;
        }
    }
}