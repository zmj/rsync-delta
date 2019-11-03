using System;
using System.Collections.Generic;

namespace Rsync.Delta.IntegrationTests
{
    public abstract class BlockSequence
    {
        private readonly int _rngSeed;
        private readonly int _blockCount;
        private readonly int _blockLength;
        private readonly int _lastBlockLength;

        protected BlockSequence(
            int rngSeed,
            int blockCount,
            int blockLength,
            int lastBlockLength)
        {
            _rngSeed = rngSeed;
            _blockCount = blockCount;
            _blockLength = blockLength;
            _lastBlockLength = lastBlockLength;
        }

        public IEnumerable<byte[]> Blocks
        {
            get
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
        }

        public int Count => _blockCount;

        public override string ToString() => GetType().Name.TrimStart('_');

        public static IEnumerable<BlockSequence> All()
        {
            yield return new _1KB();
            yield return new _1MB();
            yield return new _1MB_Plus_1();
            yield return new _1MB_Minus_1();

            // yield return new _1GB(); // 4min
            // yield return new _10GB(); // forever
        }

        public class _1KB : BlockSequence
        {
            public _1KB() : base(
                rngSeed: 5,
                blockCount: 1,
                blockLength: 2048,
                lastBlockLength: 2048)
            { }
        }

        public class _1MB : BlockSequence
        {
            public _1MB() : base(
                rngSeed: 6,
                blockCount: 512,
                blockLength: 2048,
                lastBlockLength: 2048)
            { }
        }

        public class _1MB_Plus_1 : BlockSequence
        {
            public _1MB_Plus_1() : base(
                rngSeed: 4,
                blockCount: 513,
                blockLength: 2048,
                lastBlockLength: 1)
            { }
        }

        public class _1MB_Minus_1 : BlockSequence
        {
            public _1MB_Minus_1() : base(
                rngSeed: 3,
                blockCount: 512,
                blockLength: 2048,
                lastBlockLength: 2047)
            { }
        }

        public class _1GB : BlockSequence
        {
            public _1GB() : base(
                rngSeed: 7,
                blockCount: 524288,
                blockLength: 2048,
                lastBlockLength: 2048)
            { }
        }

        public class _10GB : BlockSequence
        {
            public _10GB() : base(
                rngSeed: 8,
                blockCount: 5242880,
                blockLength: 2048,
                lastBlockLength: 2048)
            { }
        }
    }
}
