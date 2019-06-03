using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace Rsync.Delta
{
    internal class RollingHash
    {
        private const ushort _magic = 31;
        private readonly uint _blockLength;

        private ushort _a;
        private ushort _b;

        public RollingHash(uint blockLength) => _blockLength = blockLength;

        public uint Rotate(byte rollOut, byte rollIn)
        {
            _a += (ushort)(rollIn - rollOut);
            _b += (ushort)(_a - _blockLength * (rollOut + _magic));
            return Value;
        }

        public uint Hash(ReadOnlySequence<byte> sequence)
        {
            _a = 0;
            _b = 0;
            if (sequence.IsSingleSegment)
            {
                HashSpan(sequence.First.Span);
            }
            else
            {
                foreach (var memory in sequence)
                {
                    HashSpan(memory.Span);
                }
            }
            return Value;

            void HashSpan(ReadOnlySpan<byte> buffer)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    _a += (ushort)(buffer[i] + _magic);
                    _b += _a;
                }
            }
        }

        private uint Value 
        {
            get
            {
                Span<byte> buffer = stackalloc byte[4];
                BinaryPrimitives.WriteUInt16BigEndian(buffer, _b);
                BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2), _a);
                return BinaryPrimitives.ReadUInt32BigEndian(buffer);
            }
        }
    }
}