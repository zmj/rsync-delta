using System;
using System.Buffers;

namespace Rsync.Delta.Delta
{
    internal ref struct BlockEnumerator
    {
        // todo
        public byte Removed;
        public byte Added;
        public bool EndOfSequence;
        public ReadOnlySequence<byte> Block;

        public BlockEnumerator(
            in ReadOnlySequence<byte> sequence,
            int blockLength,
            bool isFinalBlock)
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            // two-tier enumeration
            // enumerate within current spans (inner loop)
            // else advance to next span
            throw new NotImplementedException();
        }
    }
}