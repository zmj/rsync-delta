﻿using System;
using System.Buffers;
using System.Diagnostics;

namespace Rsync.Delta.Models
{
    internal readonly struct EndCommand : IWritable, IReadable<EndCommand>
    {
        private const int _size = 1;
        public int Size => _size;
        public int MaxSize => _size;
        public int MinSize => _size;

        public void WriteTo(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= _size);
            buffer[0] = 0;
        }

        public OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            out EndCommand end)
        {
            if (span.Length < _size)
            {
                end = default;
                return OperationStatus.NeedMoreData;
            }
            if (span[0] == 0)
            {
                end = new EndCommand();
                return OperationStatus.Done;
            }
            end = default;
            return OperationStatus.InvalidData;
        }

        public override string ToString() => "END";
    }
}
