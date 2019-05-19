using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Rsync.Delta
{
    internal readonly struct CommandArg
    {
        public readonly ulong Value;
        public readonly int Size;
        public readonly CommandModifier Modifier;

        public const int MaxSize = 8;

        public CommandArg(ulong value)
        {
            Value = value;
            (Size, Modifier) = value switch
            {
                var _ when value <= byte.MaxValue => (1, CommandModifier.OneByte),
                var _ when value <= ushort.MaxValue => (2, CommandModifier.TwoBytes),
                var _ when value <= uint.MaxValue => (4, CommandModifier.FourBytes),
                _ => (8, CommandModifier.EightBytes),
            };
        }

        public CommandArg(ref SequenceReader<byte> reader, CommandModifier modifier)
        {
            Modifier = modifier;
            bool ok;
            switch (modifier)
            {
                case CommandModifier.OneByte:
                    ok = reader.TryRead(out byte valByte);
                    Value = valByte;
                    Size = 1;
                    break;
                case CommandModifier.TwoBytes:
                    ok = reader.TryReadBigEndian(out short valShort);
                    Value = (ushort)valShort;
                    Size = 2;
                    break;
                case CommandModifier.FourBytes:
                    ok = reader.TryReadBigEndian(out int valInt);
                    Value = (uint)valInt;
                    Size = 4;
                    break;
                case CommandModifier.EightBytes:
                    ok = reader.TryReadBigEndian(out long valLong);
                    Value = (ulong)valLong;
                    Size = 8;
                    break;
                default:
                    throw new ArgumentException(nameof(CommandModifier));
            }
            if (!ok)
            {
                throw new FormatException(nameof(CommandArg));
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= Size);
            switch (Size)
            {
                case 1:
                    buffer[0] = (byte)Value;
                    break;
                case 2:
                    BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)Value);
                    break;
                case 4:
                    BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Value);
                    break;
                default:
                    BinaryPrimitives.WriteUInt64BigEndian(buffer, Value);
                    break;
            }
        }
    }

    internal enum CommandModifier : byte
    {
        OneByte = 0,
        TwoBytes = 1,
        FourBytes = 2,
        EightBytes = 4,
    }
}