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
            ref ReadOnlySequence<byte> buffer,
            CommandModifier modifier)
        {
            Modifier = modifier;
            switch (modifier)
            {
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

        public override string ToString() => Value.ToString();
    }

    internal enum CommandModifier : byte
    {
        OneByte = 0,
        TwoBytes = 1,
        FourBytes = 2,
        EightBytes = 4,
    }
}