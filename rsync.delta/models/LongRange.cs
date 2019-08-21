namespace Rsync.Delta.Models
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
    }
}