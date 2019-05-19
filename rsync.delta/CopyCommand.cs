using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Rsync.Delta
{
    internal readonly struct CopyCommand
    {
        private const byte _baseCommand = 0x45;

        private readonly CommandArg _start;
        private readonly CommandArg _length;

        public LongRange Range => new LongRange(_start.Value, _length.Value);

        public CopyCommand(LongRange range)
        {
            _start = new CommandArg(range.Start);
            _length = new CommandArg(range.Length);
        }

        private CopyCommand(
            SequenceReader<byte> reader,
            CommandModifier startModifier,
            CommandModifier lengthModifier)
        {
            _start = new CommandArg(ref reader, startModifier);
            _length = new CommandArg(ref reader, lengthModifier);
        }

        public int Size => 1 + _start.Size + _length.Size;

        public const int MaxSize = 1 + 2 * CommandArg.MaxSize;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            byte command = (byte)(_baseCommand +
                (4 * (byte)_start.Modifier) +
                (byte)_length.Modifier);
            buffer[0] = command;
            _start.WriteTo(buffer.Slice(1));
            _length.WriteTo(buffer.Slice(1 + _start.Size));
        }

        public static bool TryParse(
            ReadOnlySequence<byte> buffer,
            out CopyCommand copy)
        {
            byte command = buffer.FirstSpan[0];
            if (command < _baseCommand ||
                command > (_baseCommand +
                    4 * (byte)CommandModifier.EightBytes +
                    (byte)CommandModifier.EightBytes))
            {
                copy = default;
                return false;
            }
            command -= _baseCommand;
            var startModifier = (command >> 2) & 0x03;
            var lengthModifier = command & 0x03;
            copy = new CopyCommand(
                new SequenceReader<byte>(buffer.Slice(1)),
                (CommandModifier)startModifier,
                (CommandModifier)lengthModifier);
            return true;
        }
    }
}