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
        IWritable<SignatureOptions>,
        IReadable<BlockSignature, SignatureOptions>
    {
        public static ushort SSize(ushort strongHashLength) =>
            (ushort)(strongHashLength + 4);

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

        public int Size(SignatureOptions options) => options.StrongHashLength + 4;

        public void WriteTo(Span<byte> buffer, SignatureOptions options)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, _rollingHash);
            _strongHash
                .Slice(0, options.StrongHashLength)
                .CopyTo(buffer.Slice(4));
        }

        public int MaxSize(SignatureOptions options) => Size(options);

        public BlockSignature? ReadFrom(
            ref ReadOnlySequence<byte> data,
            SignatureOptions options)
        {
            return new BlockSignature(ref data, options.StrongHashLength);
        }

        public bool Equals(BlockSignature other) =>
            _rollingHash == other._rollingHash &&
            _strongHash.SequenceEqual(other._strongHash);

        public override bool Equals(object? other) => 
            other is BlockSignature sig ? Equals(sig) : false;

        public override int GetHashCode() => (int)_rollingHash;
    }
}