using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct LiteralCommand : IWritable, IReadable<LiteralCommand>
    {
        private const byte _baseCommand = 0x40;

        private readonly DeltaCommandArg _lengthArg;
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
                _lengthArg = new DeltaCommandArg(length);
                _shortLiteralLength = 0;
            }
        }

        private LiteralCommand(
            ref ReadOnlySequence<byte> buffer,
            DeltaCommandModifier argModifier,
            byte shortLiteralLength)
        {
            _lengthArg = new DeltaCommandArg(ref buffer, argModifier);
            _shortLiteralLength = shortLiteralLength;
        }

        public ulong LiteralLength => _shortLiteralLength + _lengthArg.Value;

        public int Size => 1 + _lengthArg.Size;

        public int MaxSize => 1 + CommandArg.MaxSize;

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
            const byte maxCommand = _baseCommand + (byte)DeltaCommandModifier.EightBytes;
            if (command == 0 || command > maxCommand)
            {
                return null;
            }
            DeltaCommandModifier argModifier;
            byte shortLiteralLength;
            if (command <= _baseCommand)
            {
                argModifier = DeltaCommandModifier.ZeroBytes;
                shortLiteralLength = command;
            }
            else
            {
                argModifier = (DeltaCommandModifier)(command - _baseCommand);
                shortLiteralLength = 0;
            }
            data = data.Slice(1);
            return new LiteralCommand(ref data, argModifier, shortLiteralLength);
        }

        public override string ToString() => $"LITERAL: length:{LiteralLength}";
    }
}