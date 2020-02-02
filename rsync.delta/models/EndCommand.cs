using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct EndCommand : IWritable, IReadable<EndCommand>, IReadable2<EndCommand>
    {
        private const int _size = 1;
        public int Size => _size;
        public int MaxSize => _size;
        public int MinSize => _size;

        public void WriteTo(Span<byte> buffer) => buffer[0] = 0;

        public EndCommand? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            if (data.FirstByte() != 0)
            {
                return null;
            }
            data = data.Slice(Size);
            return new EndCommand();
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
