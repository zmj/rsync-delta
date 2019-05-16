using System;
using System.Collections.Generic;

namespace Rsync.Delta
{
    internal partial class BlockMatcher
    {
        internal class Builder
        {
            public SignatureOptions? Options;
            private List<BlockSignature>? _blockSignatures;

            public void Add(BlockSignature sig) 
            {
                if (_blockSignatures == null)
                {
                    _blockSignatures = new List<BlockSignature>();
                }
                _blockSignatures.Add(sig);
            }
                
            public BlockMatcher Build() 
            {
                // validate
                return new BlockMatcher(Options!.Value, _blockSignatures!.ToArray());
            }
        }
    }
}