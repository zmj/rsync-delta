using System;

namespace Rsync.Delta.Models
{
    internal readonly struct LongRange : IEquatable<LongRange>
    {
        public readonly ulong Start;
        public readonly ulong Length;

        public LongRange(ulong start, ulong length)
        {
            Start = start;
            Length = length;
        }

        public override string ToString() => $"[{Start},{checked(Start + Length)})";

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