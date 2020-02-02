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

        public CommandArg(
            CommandModifier modifier,
            ReadOnlySpan<byte> span)
        {
            Modifier = modifier;
            switch (modifier)
            {
                case CommandModifier.ZeroBytes:
                    Value = 0;
                    Size = 0;
                    break;
                case CommandModifier.OneByte:
                    Value = span[0];
                    Size = 1;
                    break;
                case CommandModifier.TwoBytes:
                    Value = BinaryPrimitives.ReadUInt16BigEndian(span);
                    Size = 2;
                    break;
                case CommandModifier.FourBytes:
                    Value = BinaryPrimitives.ReadUInt32BigEndian(span);
                    Size = 4;
                    break;
                case CommandModifier.EightBytes:
                    Value = BinaryPrimitives.ReadUInt64BigEndian(span);
                    Size = 8;
                    break;
                default:
                    throw new ArgumentException(nameof(CommandModifier));
            }
        }

        public CommandArg(
            ref ReadOnlySequence<byte> buffer,
            CommandModifier modifier)
        {
            Modifier = modifier;
            switch (modifier)
            {
                case CommandModifier.ZeroBytes:
                    Value = 0;
                    Size = 0;
                    break;
                case CommandModifier.OneByte:
                    Value = buffer.ReadByte();
                    Size = 1;
                    break;
                case CommandModifier.TwoBytes:
                    Value = buffer.ReadUShortBigEndian();
                    Size = 2;
                    break;
                case CommandModifier.FourBytes:
                    Value = buffer.ReadUIntBigEndian();
                    Size = 4;
                    break;
                case CommandModifier.EightBytes:
                    Value = buffer.ReadULongBigEndian();
                    Size = 8;
                    break;
                default:
                    throw new ArgumentException(nameof(CommandModifier));
            }
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
