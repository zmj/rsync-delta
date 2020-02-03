using System;

namespace Rsync.Delta
{
    public readonly struct SignatureOptions : IEquatable<SignatureOptions>
    {
        private readonly int? _blockLength;
        private const int _defaultBlockLength = 2048;
        public int BlockLength => _blockLength ?? _defaultBlockLength;

        private readonly int? _strongHashLength;
        private const int _defaultStrongHashLength = 32;
        public int StrongHashLength => _strongHashLength ?? _defaultStrongHashLength;

        private readonly RollingHashAlgorithm? _rollingHash;
        private const RollingHashAlgorithm _defaultRollingHash = RollingHashAlgorithm.Adler;
        public RollingHashAlgorithm RollingHash => _rollingHash ?? _defaultRollingHash;

        private readonly StrongHashAlgorithm? _strongHash;
        private const StrongHashAlgorithm _defaultStrongHash = StrongHashAlgorithm.Blake2b;
        public StrongHashAlgorithm StrongHash => _strongHash ?? _defaultStrongHash;

        public SignatureOptions(
            int blockLength = _defaultBlockLength,
            int strongHashLength = _defaultStrongHashLength,
            RollingHashAlgorithm rollingHash = _defaultRollingHash,
            StrongHashAlgorithm strongHash = _defaultStrongHash)
        {
            if (blockLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(BlockLength));
            }
            _blockLength = blockLength;

            if (strongHashLength <= 0 || strongHashLength > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(StrongHashLength));
            }
            _strongHashLength = strongHashLength;

            switch (rollingHash)
            {
                case RollingHashAlgorithm.Adler:
                case RollingHashAlgorithm.RabinKarp:
                    _rollingHash = rollingHash;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rollingHash));
            }

            switch (strongHash)
            {
                case StrongHashAlgorithm.Md4:
                case StrongHashAlgorithm.Blake2b:
                    _strongHash = strongHash;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(strongHash));
            }
        }

        public static SignatureOptions Default =>
            new SignatureOptions(
                _defaultBlockLength,
                _defaultStrongHashLength,
                _defaultRollingHash,
                _defaultStrongHash);

        public bool Equals(SignatureOptions other) =>
            BlockLength == other.BlockLength &&
            StrongHashLength == other.StrongHashLength &&
            RollingHash == other.RollingHash &&
            StrongHash == other.StrongHash;

        public override bool Equals(object? obj) =>
            obj is SignatureOptions other ? Equals(other) : false;

        public override int GetHashCode() =>
            HashCode.Combine(
                StrongHashLength,
                BlockLength,
                RollingHash,
                StrongHash);

        public static bool operator ==(
            SignatureOptions left,
            SignatureOptions right) =>
            left.Equals(right);

        public static bool operator !=(
            SignatureOptions left,
            SignatureOptions right) =>
            !left.Equals(right);
    }

    public enum RollingHashAlgorithm
    {
        Adler = 0x30,
        RabinKarp = 0x40,
    }

    public enum StrongHashAlgorithm
    {
        Md4 = 0x06,
        Blake2b = 0x07,
    }
}
