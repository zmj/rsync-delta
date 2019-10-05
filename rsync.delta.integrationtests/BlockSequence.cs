using System;
using System.Collections;
using System.Collections.Generic;

namespace Rsync.Delta.IntegrationTests
{
    public class BlockSequence : IEnumerable<byte[]>
    {
        private readonly int _rngSeed;
        private readonly int _blockCount;
        private readonly int _blockLength;
        private readonly int _lastBlockLength;
        private readonly string _name;

        public BlockSequence(
            int rngSeed,
            int blockCount,
            int blockLength,
            int lastBlockLength,
            string name)
        {
            _rngSeed = rngSeed;
            _blockCount = blockLength;
            _blockLength = blockLength;
            _lastBlockLength = lastBlockLength;
            _name = name.TrimStart('_');
        }

        public IEnumerator<byte[]> GetEnumerator()
        {
            var rng = new Random(_rngSeed);
            for (int i = 0; i < _blockCount; i++)
            {
                int len = i == _blockCount - 1 ? _lastBlockLength : _blockLength;
                var buffer = new byte[len];
                rng.NextBytes(buffer);
                yield return buffer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => _name;

        public static IEnumerable<BlockSequence> All()
        {
            yield return _1KB;
        }

        private static readonly BlockSequence _1KB = new BlockSequence(
            rngSeed: 5,
            blockCount: 1,
            blockLength: 2048,
            lastBlockLength: 1024,
            nameof(_1KB));
    }
}