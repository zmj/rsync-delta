using System;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace Rsync.Delta
{
    internal class RollingHash
    {
        private const ushort _magic = 31;

        private ushort _a;
        private ushort _b;

        public void Hash(ReadOnlySpan<byte> buffer)
        {
            unchecked
            {
                for (int i=0; i<buffer.Length; i++)
                {
                    _a += (ushort)(buffer[i] + _magic);
                    _b += _a;
                }
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer, _b);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2), _a);
        }

        public uint Value 
        {
            get
            {
                Span<byte> buffer = stackalloc byte[4];
                WriteTo(buffer);
                return BinaryPrimitives.ReadUInt32BigEndian(buffer);
            }
        }
    }
}