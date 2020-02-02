using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct CommandArg : IWritable
    {
        public readonly ulong Value;
        public int Size { get; }
        public readonly CommandModifier Modifier;

        public const int MaxSize = 8;
        public const int MinSize = 1;

        public CommandArg(ulong value)
        {
            Value = value;
            if (value <= byte.MaxValue)
            {
                Size = 1;
                Modifier = CommandModifier.OneByte;
            }
            else if (value <= ushort.MaxValue)
            {
                Size = 2;
                Modifier = CommandModifier.TwoBytes;
            }
            else if (value <= uint.MaxValue)
            {
                Size = 4;
                Modifier = CommandModifier.FourBytes;
            }
            else
            {
                Size = 8;
                Modifier = CommandModifier.EightBytes;
            }
        }

        private CommandArg(ulong value, int size, CommandModifier modifier)
        {
            Value = value;
            Size = size;
            Modifier = modifier;
        }

        public static OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            CommandModifier modifier,
            out CommandArg arg)
        {
            bool ok;
            ulong value;
            int size;
            switch (modifier)
            {
                case CommandModifier.ZeroBytes:
                    size = 0;
                    ok = true;
                    value = 0;
                    break;
                case CommandModifier.OneByte:
                    size = 1;
                    ok = span.Length >= 1;
                    value = ok ? span[0] : 0UL;
                    break;
                case CommandModifier.TwoBytes:
                    size = 2;
                    ok = BinaryPrimitives.TryReadUInt16BigEndian(span, out var valShort);
                    value = valShort;
                    break;
                case CommandModifier.FourBytes:
                    size = 4;
                    ok = BinaryPrimitives.TryReadUInt32BigEndian(span, out var valInt);
                    value = valInt;
                    break;
                case CommandModifier.EightBytes:
                    size = 8;
                    ok = BinaryPrimitives.TryReadUInt64BigEndian(span, out value);
                    break;
                default:
                    throw new ArgumentException($"{nameof(CommandModifier)}.{modifier}");
            }
            if (!ok)
            {
                arg = default;
                return OperationStatus.NeedMoreData;
            }
            arg = new CommandArg(value, size, modifier);
            return OperationStatus.Done;
        }

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            switch (Size)
            {
                case 0:
                    break;
                case 1:
                    buffer[0] = (byte)Value;
                    break;
                case 2:
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)Value);
                    break;
                case 4:
                    BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Value);
                    break;
                case 8:
                    BinaryPrimitives.WriteUInt64BigEndian(buffer, Value);
                    break;
                default:
                    throw new ArgumentException(nameof(Size));
            }
        }

        public override string ToString() => Value.ToString();
    }

    internal enum CommandModifier : byte
    {
        ZeroBytes = 0,
        OneByte = 1,
        TwoBytes = 2,
        FourBytes = 3,
        EightBytes = 4,
    }
}
