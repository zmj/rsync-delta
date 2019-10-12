using System;

namespace Rsync.Delta
{
    public readonly struct SignatureOptions : IEquatable<SignatureOptions>
    {
        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);

        public int BlockLength { get; }
        public int StrongHashLength { get; }

        public SignatureOptions(int blockLength, int strongHashLength)
        {
            if (blockLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(BlockLength));
            }
            if (strongHashLength <= 0 || strongHashLength > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(StrongHashLength));
            }
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }

        public bool Equals(SignatureOptions other) =>
            BlockLength == other.BlockLength &&
            StrongHashLength == other.StrongHashLength;

        public override bool Equals(object? obj) =>
            obj is SignatureOptions other ? Equals(other) : false;

        public override int GetHashCode() =>
            HashCode.Combine(StrongHashLength, BlockLength);

        public static bool operator ==(
            SignatureOptions left,
            SignatureOptions right) =>
            left.Equals(right);

        public static bool operator !=(
            SignatureOptions left,
            SignatureOptions right) =>
            !left.Equals(right);
    }
}
