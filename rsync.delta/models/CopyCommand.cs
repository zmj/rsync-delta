﻿using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct CopyCommand : IWritable, IReadable<CopyCommand>
    {
        private const byte _baseCommand = 0x40;

        private readonly CommandArg _start;
        private readonly CommandArg _length;

        public LongRange Range => new LongRange(_start.Value, _length.Value);

        public CopyCommand(LongRange range)
        {
            _start = new CommandArg(range.Start);
            _length = new CommandArg(range.Length);
        }

        private CopyCommand(
            ref ReadOnlySequence<byte> buffer,
            CommandModifier startModifier,
            CommandModifier lengthModifier)
        {
            _start = new CommandArg(ref buffer, startModifier);
            _length = new CommandArg(ref buffer, lengthModifier);
        }

        public int Size => 1 + _start.Size + _length.Size;

        public int MaxSize => 1 + 2 * CommandArg.MaxSize;

        public int MinSize => 1 + 2 * CommandArg.MinSize;

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

        public CopyCommand? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            byte command = data.FirstByte();
            const byte minCommand = _baseCommand +
                4 * (byte)CommandModifier.OneByte +
                (byte)CommandModifier.OneByte;
            const byte maxCommand = _baseCommand +
                4 * (byte)CommandModifier.EightBytes +
                (byte)CommandModifier.EightBytes;
            if (command < minCommand || command > maxCommand)
            {
                return null;
            }
            data = data.Slice(1);
            command -= _baseCommand;
            var startModifier = command >> 2;
            var lengthModifier = command & 0x03;
            return new CopyCommand(
                ref data,
                (CommandModifier)startModifier,
                (CommandModifier)lengthModifier);
        }

        public override string ToString() => $"COPY: start:{_start} length:{_length}";
    }
}
