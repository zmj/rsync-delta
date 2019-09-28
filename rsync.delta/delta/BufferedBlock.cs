using System.Buffers;

namespace Rsync.Delta.Delta
{
    internal readonly struct BufferedBlock
    {
        public readonly ReadOnlySequence<byte> PendingLiteral;
        public readonly ReadOnlySequence<byte> CurrentBlock;

        public BufferedBlock(
            in ReadOnlySequence<byte> pendingLiteral,
            in ReadOnlySequence<byte> currentBlock)
        {
            PendingLiteral = pendingLiteral;
            CurrentBlock = currentBlock;
        }
    }
}