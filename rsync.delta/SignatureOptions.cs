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

        private readonly RollingHashAlgorithm? _rollingHashAlgorithm;
        private const RollingHashAlgorithm _defaultRollingHashAlgorithm = RollingHashAlgorithm.RabinKarp;
        public RollingHashAlgorithm RollingHashAlgorithm => _rollingHashAlgorithm ?? _defaultRollingHashAlgorithm;

        private readonly StrongHashAlgorithm? _strongHashAlgorithm;
        private const StrongHashAlgorithm _defaultStrongHashAlgorithm = StrongHashAlgorithm.Blake2b;
        public StrongHashAlgorithm StrongHash => _strongHashAlgorithm ?? _defaultStrongHashAlgorithm;

        public SignatureOptions(
            int blockLength = _defaultBlockLength,
            int strongHashLength = _defaultStrongHashLength,
            RollingHashAlgorithm rollingHashAlgorithm = _defaultRollingHashAlgorithm,
            StrongHashAlgorithm strongHashAlgorithm = _defaultStrongHashAlgorithm)
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

            switch (rollingHashAlgorithm)
            {
                case RollingHashAlgorithm.Adler:
                case RollingHashAlgorithm.RabinKarp:
                    _rollingHashAlgorithm = rollingHashAlgorithm;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rollingHashAlgorithm));
            }

            switch (strongHashAlgorithm)
            {
                case StrongHashAlgorithm.Md4:
                case StrongHashAlgorithm.Blake2b:
                    _strongHashAlgorithm = strongHashAlgorithm;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(strongHashAlgorithm));
            }
        }

        public bool Equals(SignatureOptions other) =>
            BlockLength == other.BlockLength &&
            StrongHashLength == other.StrongHashLength &&
            RollingHashAlgorithm == other.RollingHashAlgorithm &&
            StrongHash == other.StrongHash;

        public override bool Equals(object? obj) =>
            obj is SignatureOptions other ? Equals(other) : false;

        public override int GetHashCode() =>
            HashCode.Combine(
                StrongHashLength,
                BlockLength,
                RollingHashAlgorithm,
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
