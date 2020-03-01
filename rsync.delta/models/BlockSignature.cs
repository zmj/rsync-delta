using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Rsync.Delta.Hash;

namespace Rsync.Delta.Models
{
    internal readonly struct BlockSignature
        <TRollingHashAlgorithm, TStrongHashAlgorithm> :
            IEquatable<BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm>>,
            IWritable<SignatureOptions>,
            IReadable<BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm>, SignatureOptions>
        where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
        where TStrongHashAlgorithm : IStrongHashAlgorithm
    {
        private readonly int _rollingHash;

        private readonly long _eagerStrongHash0;
        private readonly long _eagerStrongHash1;
        private readonly long _eagerStrongHash2;
        private readonly long _eagerStrongHash3;

        private readonly Delta.BlockMatcher<TRollingHashAlgorithm, TStrongHashAlgorithm>? _blockMatcher;
        private readonly long _blockStart;
        private readonly long _blockLength;

        public BlockSignature(int rollingHash, ReadOnlySpan<byte> strongHash)
        {
            _rollingHash = rollingHash;
            SplitStrongHash(
                strongHash,
                out _eagerStrongHash0,
                out _eagerStrongHash1,
                out _eagerStrongHash2,
                out _eagerStrongHash3);

            _blockMatcher = null;
            _blockStart = default;
            _blockLength = default;
        }

        public BlockSignature(
            int rollingHash,
            Delta.BlockMatcher<TRollingHashAlgorithm, TStrongHashAlgorithm> blockMatcher,
            long blockStart,
            long blockLength)
        {
            _rollingHash = rollingHash;
            _blockMatcher = blockMatcher;
            _blockStart = blockStart;
            _blockLength = blockLength;

            _eagerStrongHash0 = default;
            _eagerStrongHash1 = default;
            _eagerStrongHash2 = default;
            _eagerStrongHash3 = default;
        }

        private static void SplitStrongHash(
            ReadOnlySpan<byte> strongHash,
            out long sh0, out long sh1, out long sh2, out long sh3)
        {
            Debug.Assert(strongHash.Length <= 32);
            if (strongHash.Length < 32)
            {
                Span<byte> tmp = stackalloc byte[32];
                strongHash.CopyTo(tmp);
                SplitStrongHash(tmp, out sh0, out sh1, out sh2, out sh3);
                return;
            }
            sh0 = BinaryPrimitives.ReadInt64BigEndian(strongHash);
            sh1 = BinaryPrimitives.ReadInt64BigEndian(strongHash.Slice(8));
            sh2 = BinaryPrimitives.ReadInt64BigEndian(strongHash.Slice(16));
            sh3 = BinaryPrimitives.ReadInt64BigEndian(strongHash.Slice(24));
        }

        private static void CombineStrongHash(
            Span<byte> strongHash,
            long sh0, long sh1, long sh2, long sh3)
        {
            Debug.Assert(strongHash.Length <= 32);
            if (strongHash.Length < 32)
            {
                Span<byte> tmp = stackalloc byte[32];
                CombineStrongHash(tmp, sh0, sh1, sh2, sh3);
                tmp.Slice(0, strongHash.Length).CopyTo(strongHash);
                return;
            }
            BinaryPrimitives.WriteInt64BigEndian(strongHash, sh0);
            BinaryPrimitives.WriteInt64BigEndian(strongHash.Slice(8), sh1);
            BinaryPrimitives.WriteInt64BigEndian(strongHash.Slice(16), sh2);
            BinaryPrimitives.WriteInt64BigEndian(strongHash.Slice(24), sh3);
        }

        public int Size(SignatureOptions options) => options.StrongHashLength + 4;

        public void WriteTo(Span<byte> buffer, SignatureOptions options)
        {
            Debug.Assert(_blockMatcher == null);
            Debug.Assert(buffer.Length >= Size(options));
            BinaryPrimitives.WriteInt32BigEndian(buffer, _rollingHash);
            Span<byte> strongHash = stackalloc byte[options.StrongHashLength];
            CombineStrongHash(
                strongHash,
                _eagerStrongHash0,
                _eagerStrongHash1,
                _eagerStrongHash2,
                _eagerStrongHash3);
            strongHash.CopyTo(buffer.Slice(4));
        }

        public int MaxSize(SignatureOptions options) => Size(options);

        public int MinSize(SignatureOptions options) => Size(options);

        public OperationStatus ReadFrom(
            ReadOnlySpan<byte> span,
            SignatureOptions options,
            out BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm> sig)
        {
            if (span.Length < Size(options))
            {
                sig = default;
                return OperationStatus.NeedMoreData;
            }
            var rollingHash = BinaryPrimitives.ReadInt32BigEndian(span);
            var strongHash = span.Slice(4, options.StrongHashLength);
            sig = new BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm>
                (rollingHash, strongHash);
            return OperationStatus.Done;
        }

        public bool Equals(BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm> other)
        {
            Debug.Assert(_blockMatcher == null || other._blockMatcher == null);
            if (_rollingHash != other._rollingHash)
            {
                return false;
            }

            if (_blockMatcher != null)
            {
                return StrongHashEquals(eager: other, lazy: this);
            }
            else if (other._blockMatcher != null)
            {
                return StrongHashEquals(eager: this, lazy: other);
            }
            return _eagerStrongHash0 == other._eagerStrongHash0 &&
                _eagerStrongHash1 == other._eagerStrongHash1 &&
                _eagerStrongHash2 == other._eagerStrongHash2 &&
                _eagerStrongHash3 == other._eagerStrongHash3;
        }

        private static bool StrongHashEquals(
            in BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm> eager,
            in BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm> lazy)
        {
            var lazyStrongHash = lazy._blockMatcher!
                .GetStrongHash(lazy._blockStart, lazy._blockLength)
                .Span;
            Span<byte> eagerStrongHash = stackalloc byte[lazyStrongHash.Length];
            CombineStrongHash(
                eagerStrongHash,
                eager._eagerStrongHash0,
                eager._eagerStrongHash1,
                eager._eagerStrongHash2,
                eager._eagerStrongHash3);
            return lazyStrongHash.SequenceEqual(eagerStrongHash);
        }

        public override bool Equals(object? other) =>
            other is BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm> sig
                ? Equals(sig) : false;

        public override int GetHashCode() => _rollingHash;
    }
}
