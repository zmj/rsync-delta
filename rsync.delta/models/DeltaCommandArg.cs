using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct DeltaCommandArg : IWritable
    {
        public readonly ulong Value;
        public int Size { get; }
        public readonly DeltaCommandModifier Modifier;

        public const int MaxSize = 8;

        public DeltaCommandArg(ulong value)
        {
            Value = value;
            if (value <= byte.MaxValue)
            {
                Size = 1;
                Modifier = DeltaCommandModifier.OneByte;
            }
            else if (value <= ushort.MaxValue)
            {
                Size = 2;
                Modifier = DeltaCommandModifier.TwoBytes;
            }
            else if (value <= uint.MaxValue)
            {
                Size = 4;
                Modifier = DeltaCommandModifier.FourBytes;
            }
            else
            {
                Size = 8;
                Modifier = DeltaCommandModifier.EightBytes;
            }
        }

        public DeltaCommandArg(
            ref ReadOnlySequence<byte> buffer,
            DeltaCommandModifier modifier)
        {
            Modifier = modifier;
            switch (modifier)
            {
                case DeltaCommandModifier.ZeroBytes:
                    Value = 0;
                    Size = 0;
                    break;
                case DeltaCommandModifier.OneByte:
                    Value = buffer.ReadByte();
                    Size = 1;
                    break;
                case DeltaCommandModifier.TwoBytes:
                    Value = buffer.ReadUShortBigEndian();
                    Size = 2;
                    break;
                case DeltaCommandModifier.FourBytes:
                    Value = buffer.ReadUIntBigEndian();
                    Size = 4;
                    break;
                case DeltaCommandModifier.EightBytes:
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

    internal enum DeltaCommandModifier : byte
    {
        ZeroBytes = 0,
        OneByte = 1,
        TwoBytes = 2,
        FourBytes = 3,
        EightBytes = 4,
    }
}