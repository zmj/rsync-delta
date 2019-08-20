using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct BlockSignature : 
        IEquatable<BlockSignature>,
        IWritable
    {
        public static ushort SSize(ushort strongHashLength) =>
            (ushort)(strongHashLength + 4);
        public int Size => (int)SSize((ushort)_strongHash.Length); // todo cleanup

        private readonly uint _eagerRollingHash;
        private readonly ReadOnlyMemory<byte> _eagerStrongHash; // fieldify
        private readonly Delta.LazyBlockSignature? _lazySignature;

        private uint _rollingHash =>
            _lazySignature?.RollingHash ?? _eagerRollingHash;
        
        private ReadOnlySpan<byte> _strongHash =>
            (_lazySignature?.StrongHash ?? _eagerStrongHash).Span;

        public BlockSignature(uint rollingHash, ReadOnlyMemory<byte> strongHash)
        {
            _eagerRollingHash = rollingHash;
            _eagerStrongHash = strongHash;
            _lazySignature = null;
        }

        public BlockSignature(ref ReadOnlySequence<byte> buffer, int strongHashLength)
        {
            _eagerRollingHash = buffer.ReadUIntBigEndian();

            Span<byte> tmp = stackalloc byte[strongHashLength];
            _eagerStrongHash = buffer.ReadN(tmp).ToArray(); // store in fields
            _lazySignature = null;
        }

        public BlockSignature(Delta.LazyBlockSignature lazySig)
        {
            _lazySignature = lazySig;
            _eagerRollingHash = default;
            _eagerStrongHash = default;
        }

        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, _rollingHash);
            _strongHash.CopyTo(buffer.Slice(4));
        }

        public bool Equals(BlockSignature other) =>
            _rollingHash == other._rollingHash &&
            _strongHash.SequenceEqual(other._strongHash);

        public override bool Equals(object? other) => 
            other is BlockSignature sig ? Equals(sig) : false;

        public override int GetHashCode() => (int)_rollingHash;
    }
}