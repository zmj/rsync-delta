using System;
using System.Buffers;
using System.Buffers.Binary;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta
{
    public readonly struct BlockSignature : 
        IEquatable<BlockSignature>, IWritable
    {
        public static ushort SSize(ushort strongHashLength) =>
            (ushort)(strongHashLength + 4);
        public int Size => (int)SSize((ushort)_strongHash!.Value.Length); // todo cleanup

        private readonly uint _rollingHash;
        private readonly ReadOnlyMemory<byte>? _strongHash; // rent from matcher
        private readonly Func<ReadOnlyMemory<byte>>? _lazyStrongHash;

        private ReadOnlySpan<byte> StrongHash => 
            (_strongHash ?? _lazyStrongHash!()).Span;

        public BlockSignature(uint rollingHash, ReadOnlyMemory<byte> strongHash)
        {
            _rollingHash = rollingHash;
            _strongHash = strongHash;
            _lazyStrongHash = null;
        }

        public BlockSignature(ref ReadOnlySequence<byte> buffer, int strongHashLength)
        {
            _rollingHash = buffer.ReadUIntBigEndian();

            Span<byte> tmp = stackalloc byte[strongHashLength];
            _strongHash = buffer.ReadN(tmp).ToArray();
            _lazyStrongHash = null;
        }

        public BlockSignature(uint rollingHash, Func<ReadOnlyMemory<byte>> lazyStrongHash)
        {
            _rollingHash = rollingHash;
            _strongHash = null;
            _lazyStrongHash = lazyStrongHash;
        }
        
        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, _rollingHash);
            StrongHash.CopyTo(buffer.Slice(4));
        }

        public bool Equals(BlockSignature other) =>
            _rollingHash == other._rollingHash &&
            StrongHash.SequenceEqual(other.StrongHash);

        public override bool Equals(object? other) =>
            other is BlockSignature sig ? Equals(sig) : false;

        public override int GetHashCode() => (int)_rollingHash;
    }
}