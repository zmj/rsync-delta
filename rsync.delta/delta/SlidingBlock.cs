using System;
using System.Buffers;

namespace Rsync.Delta.Delta
{
    internal ref struct SlidingBlock
    {
        private ref struct Position
        {
            public ReadOnlySequence<byte>.Enumerator MemoryEnumerator;
            public long SpanPositionInSequence;
            public ReadOnlySpan<byte> Span;
            public int PositionInSpan;
        }

        public SlidingBlock(
            in ReadOnlySequence<byte> sequence,
            int blockLength,
            bool isFinalBlock)
        {
            throw new NotImplementedException();
        }

        public bool TryAdvance(
            out long start, out long length, 
            out byte removed, out byte added)
        {
            // two-tier enumeration
            // enumerate within current spans (inner loop)
            // else advance to next span
            throw new NotImplementedException();
        }
    }
}