using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    public readonly struct BlockSignature
    {
        public static ushort Size(ushort strongHashLength) =>
            (ushort)(strongHashLength + 4);

        public readonly uint RollingHash;
        public readonly Memory<byte> StrongHash; // rent from matcher

        public BlockSignature(uint rollingHash, Memory<byte> strongHash)
        {
            RollingHash = rollingHash;
            StrongHash = strongHash;
        }

        public BlockSignature(ref ReadOnlySequence<byte> buffer, int strongHashLength)
        {
            RollingHash = buffer.ReadUIntBigEndian();
            
            Span<byte> tmp = stackalloc byte[strongHashLength];
            StrongHash = buffer.ReadN(tmp).ToArray();
        }
        
        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, RollingHash);
            StrongHash.Span.CopyTo(buffer.Slice(4));
        }
    }
}