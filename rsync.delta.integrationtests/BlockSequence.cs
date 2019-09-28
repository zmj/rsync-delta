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
    }
}