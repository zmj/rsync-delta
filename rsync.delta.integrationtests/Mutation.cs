using System;
using System.Collections.Generic;

namespace Rsync.Delta.IntegrationTests
{
    public class Mutation
    {
        private readonly Func<byte[], byte[]> _mutation;
        private readonly string _name;

        public IEnumerable<byte[]> ApplyTo(
            IEnumerable<byte[]> blocks,
            int index)
        {
            int i = 0;
            foreach (var block in blocks)
            {
                yield return i++ == index ?
                    _mutation(block) :
                    block;
            }
        }

        public override string ToString() => _name;

        public static IEnumerable<Mutation> All()
        {
            yield break;
        }
    }
}