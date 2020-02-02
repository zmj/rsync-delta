﻿using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct CopyCommand : IWritable, IReadable<CopyCommand>, IReadable2<CopyCommand>
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

        private CopyCommand(CommandArg startArg, CommandArg lengthArg)
        {
            _start = startArg;
            _length = lengthArg;
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

        private const int _maxSize = 1 + 2 * CommandArg.MaxSize;
        public int MaxSize => _maxSize;

        private const int _minSize = 1 + 2 * CommandArg.MinSize;
        public int MinSize => _minSize;

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

        public OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            out CopyCommand copy)
        {
            if (span.Length < _minSize)
            {
                copy = default;
                return OperationStatus.NeedMoreData;
            }
            byte command = span[0];
            const byte minCommand = _baseCommand +
                4 * (byte)CommandModifier.OneByte +
                (byte)CommandModifier.OneByte;
            const byte maxCommand = _baseCommand +
                4 * (byte)CommandModifier.EightBytes +
                (byte)CommandModifier.EightBytes;
            if (command < minCommand || command > maxCommand)
            {
                copy = default;
                return OperationStatus.InvalidData;
            }
            command -= _baseCommand;

            var startModifier = (CommandModifier)(command >> 2);
            var opStatus = CommandArg.ReadFrom(
                span.Slice(1),
                startModifier,
                out var startArg);
            if (opStatus != OperationStatus.Done)
            {
                copy = default;
                return opStatus;
            }

            var lengthModifier = (CommandModifier)(command & 0x03);
            opStatus = CommandArg.ReadFrom(
                span.Slice(1 + startArg.Size),
                lengthModifier,
                out var lengthArg);
            if (opStatus != OperationStatus.Done)
            {
                copy = default;
                return opStatus;
            }

            copy = new CopyCommand(startArg, lengthArg);
            return OperationStatus.Done;
        }

        public CopyCommand? TryReadFrom(ReadOnlySpan<byte> span)
        {
            Debug.Assert(span.Length >= MinSize);
            byte command = span[0];
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
            command -= _baseCommand;
            var startModifier = (CommandModifier)(command >> 2);
            var startArg = new CommandArg(startModifier, span.Slice(1));
            var lengthModifier = (CommandModifier)(command & 0x03);
            var lengthArg = new CommandArg(
                lengthModifier,
                span.Slice(1 + startArg.Size));
            return new CopyCommand(startArg, lengthArg);
        }

        public override string ToString() => $"COPY: start:{_start} length:{_length}";
    }
}
