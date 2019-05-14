using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal readonly struct DeltaWriter
    {
        private readonly BlockMatcher _blocks;
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;

        public DeltaWriter(BlockMatcher blocks, PipeReader reader, PipeWriter writer)
        {
            _blocks = blocks;
            _reader = reader;
            _writer = writer;
        }

        public ValueTask Write(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}