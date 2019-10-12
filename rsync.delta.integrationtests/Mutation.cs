using System;
using System.Collections.Generic;

namespace Rsync.Delta.IntegrationTests
{
    public abstract class Mutation
    {
        public abstract byte[] Mutate(byte[] block);

        public IEnumerable<byte[]> ApplyTo(
            IEnumerable<byte[]> blocks,
            int index)
        {
            int i = 0;
            foreach (var block in blocks)
            {
                yield return i++ == index ?
                    Mutate(block) :
                    block;
            }
        }

        public override string ToString() => GetType().Name;

        public static IEnumerable<Mutation> All()
        {
            yield return new NoChange();
            yield return new TrimStart(1);
            yield return new TrimStart(2);
            yield return new TrimStart(10);
            yield return new TrimEnd(1);
            yield return new TrimEnd(2);
            yield return new TrimEnd(10);
            yield return new IncrementAll();
        }

        private class NoChange : Mutation
        {
            public override byte[] Mutate(byte[] block) => block;
        }

        private class TrimStart : Mutation
        {
            private readonly int _n;
            public TrimStart(int n) => _n = n;
            public override byte[] Mutate(byte[] block)
            {
                int n = _n > block.Length ? block.Length : _n;
                var mutated = new byte[block.Length - n];
                block.AsSpan().Slice(n).CopyTo(mutated.AsSpan());
                return mutated;
            }
            public override string ToString() => base.ToString() + '_' + _n;
        }

        private class TrimEnd : Mutation
        {
            private readonly int _n;
            public TrimEnd(int n) => _n = n;
            public override byte[] Mutate(byte[] block)
            {
                int n = _n > block.Length ? block.Length : _n;
                var mutated = new byte[block.Length - n];
                block.AsSpan().Slice(0, block.Length - n).CopyTo(mutated.AsSpan());
                return mutated;
            }
            public override string ToString() => base.ToString() + '_' + _n;
        }

        private class IncrementAll : Mutation
        {
            public override byte[] Mutate(byte[] block)
            {
                for (int i = 0; i < block.Length; i++)
                {
                    block[i]++;
                }
                return block;
            }
        }
    }
}