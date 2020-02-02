using System;
using System.Buffers;
using System.Diagnostics;

namespace Rsync.Delta.Models
{
    internal readonly struct LiteralCommand : IWritable, IReadable<LiteralCommand>
    {
        private const byte _baseCommand = 0x40;

        private readonly CommandArg _lengthArg;
        private readonly byte _shortLiteralLength;

        public LiteralCommand(ulong length)
        {
            if (length <= _baseCommand)
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

        private LiteralCommand(CommandArg arg)
        {
            _lengthArg = arg;
            _shortLiteralLength = 0;
        }

        public ulong LiteralLength => _shortLiteralLength + _lengthArg.Value;

        public int Size => 1 + _lengthArg.Size;

        private const int _maxSize = 1 + CommandArg.MaxSize;
        public int MaxSize => _maxSize;

        private const int _minSize = 1;
        public int MinSize => _minSize;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            byte command = _shortLiteralLength > 0 ?
                _shortLiteralLength :
                (byte)(_baseCommand + (byte)_lengthArg.Modifier);
            buffer[0] = command;
            _lengthArg.WriteTo(buffer.Slice(1));
        }

        public OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            out LiteralCommand literal)
        {
            if (span.Length < _minSize)
            {
                literal = default;
                return OperationStatus.NeedMoreData;
            }
            byte command = span[0];
            const byte maxCommand = _baseCommand + (byte)CommandModifier.EightBytes;
            if (command == 0 || command > maxCommand)
            {
                literal = default;
                return OperationStatus.InvalidData;
            }
            else if (command < _baseCommand)
            {
                literal = new LiteralCommand(length: command);
                return OperationStatus.Done;
            }

            var argModifier = (CommandModifier)(command - _baseCommand);
            var opStatus = CommandArg.ReadFrom(
                span.Slice(1),
                argModifier,
                out var arg);
            if (opStatus != OperationStatus.Done)
            {
                literal = default;
                return opStatus;
            }
            literal = new LiteralCommand(arg);
            return OperationStatus.Done;
        }

        public override string ToString() => $"LITERAL: length:{LiteralLength}";
    }
}
