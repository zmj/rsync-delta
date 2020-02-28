using System;

namespace Rsync.Delta.Models
{
    internal readonly struct LongRange : IEquatable<LongRange>
    {
        public readonly long Start;
        public readonly long Length;

        public LongRange(long start, long length)
        {
            if (start < 0) 
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            Start = start;
            Length = length;
        }

        public bool TryAppend(LongRange other, out LongRange appended)
        {
            if (Length == 0)
            {
                appended = other;
                return true;
            }
            checked
            {
                if (Start + Length == other.Start)
                {
                    appended = new LongRange(
                        start: Start,
                        length: Length + other.Length);
                    return true;
                }
            }
            appended = default;
            return false;
        }

        public override string ToString() => $"[{Start},{Start + Length})";

        public bool Equals(LongRange other) =>
            Start == other.Start &&
            Length == other.Length;

        public override bool Equals(object? obj) =>
            obj is LongRange other ? Equals(other) : false;

        public override int GetHashCode() => System.HashCode.Combine(Start, Length);

        public static bool operator ==(LongRange left, LongRange right) =>
            left.Equals(right);

        public static bool operator !=(LongRange left, LongRange right) =>
            !left.Equals(right);
    }
}