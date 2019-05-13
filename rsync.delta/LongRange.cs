namespace Rsync.Delta
{
    internal readonly struct LongRange
    {
        public readonly ulong Start;
        public readonly ulong Length;

        public LongRange(ulong start, ulong length)
        {
            Start = start;
            Length = length;
        }

        public bool TryAppendTo(ref LongRange? other)
        {
            if (!other.HasValue)
            {
                other = this;
                return true;
            }
            // check for overflows
            if (other.Value.Start + other.Value.Length != Start)
            {
                return false;
            }
            other = new LongRange(other.Value.Start, other.Value.Length + Length);
            return true;
        }
    }
}