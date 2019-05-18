using System.Buffers;
using System.Linq;

namespace Rsync.Delta
{
    internal partial class BlockMatcher
    {
        // match weak sum, match strong sum
        // construct with builder?
        public readonly uint BlockLength;
        private readonly uint _strongHashLength;
        private readonly BlockSignature[] _blockSignatures; // memory?

        public BlockMatcher(
            SignatureOptions options,
            BlockSignature[] blockSignatures)
        {
            BlockLength = options.BlockLength;
            _strongHashLength = options.StrongHashLength;
            _blockSignatures = blockSignatures;
        }

        public LongRange? MatchBlock(ReadOnlySequence<byte> buffer3)
        {
            // todo: rolling hash optimization
            byte[] buffer = buffer3.ToArray();
            byte[] hash = Blake2.Blake2b.Hash(buffer);
            for (int i=0; i<_blockSignatures.Length; i++)
            {
                var sig = _blockSignatures[i];
                if (hash.SequenceEqual(sig.StrongHash.ToArray())) // sequencecompareto
                {
                    return new LongRange(
                        start: (ulong)i * BlockLength, // overflow check
                        length: (uint)buffer.Length);
                }
            }
            return null;
        }
    }
}