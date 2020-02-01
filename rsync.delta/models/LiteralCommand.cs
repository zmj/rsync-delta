using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct LiteralCommand : IWritable, IReadable<LiteralCommand>
    {
        private const byte _baseCommand = 0x40;

        private readonly CommandArg _lengthArg;
        private readonly byte _shortLiteralLength;

        public LiteralCommand(ulong length)
        {
            if (length <= 64)
            {
                _shortLiteralLength = (byte)length;
                _lengthArg = default;
            }
            else
            {
                _lengthArg = new CommandArg(length);
                _shortLiteralLength = 0;
            }
        }

        private LiteralCommand(
            ref ReadOnlySequence<byte> buffer,
            CommandModifier argModifier,
            byte shortLiteralLength)
        {
            _lengthArg = new CommandArg(ref buffer, argModifier);
            _shortLiteralLength = shortLiteralLength;
        }

        public ulong LiteralLength => _shortLiteralLength + _lengthArg.Value;

        public int Size => 1 + _lengthArg.Size;

        public int MaxSize => 1 + CommandArg.MaxSize;

        public int MinSize => 1;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            byte command = _shortLiteralLength > 0 ?
                _shortLiteralLength :
                (byte)(_baseCommand + (byte)_lengthArg.Modifier);
            buffer[0] = command;
            _lengthArg.WriteTo(buffer.Slice(1));
        }

        public LiteralCommand? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            byte command = data.FirstByte();
            const byte maxCommand = _baseCommand + (byte)CommandModifier.EightBytes;
            if (command == 0 || command > maxCommand)
            {
                return null;
            }
            CommandModifier argModifier;
            byte shortLiteralLength;
            if (command <= _baseCommand)
            {
                argModifier = CommandModifier.ZeroBytes;
                shortLiteralLength = command;
            }
            else
            {
                argModifier = (CommandModifier)(command - _baseCommand);
                shortLiteralLength = 0;
            }
            data = data.Slice(1);
            return new LiteralCommand(ref data, argModifier, shortLiteralLength);
        }

        public override string ToString() => $"LITERAL: length:{LiteralLength}";
    }
}
