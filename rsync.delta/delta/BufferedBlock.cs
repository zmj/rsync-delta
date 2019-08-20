using System.Buffers;

namespace Rsync.Delta.Delta
{
    internal readonly struct BufferedBlock
    {
        public readonly ReadOnlySequence<byte> PendingLiteral;
        public readonly ReadOnlySequence<byte> CurrentBlock;

        public BufferedBlock(
            ReadOnlySequence<byte> pendingLiteral, 
            ReadOnlySequence<byte> currentBlock)
        {
            PendingLiteral = pendingLiteral;
            CurrentBlock = currentBlock;
        }
    }
}