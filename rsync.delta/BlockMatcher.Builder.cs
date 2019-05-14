using System;

namespace Rsync.Delta
{
    internal readonly partial struct BlockMatcher
    {
        internal readonly struct Builder
        {
            public void Add(BlockSignature sig) { }
            public BlockMatcher Build() => new BlockMatcher();
        }
    }
}