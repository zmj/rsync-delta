using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Signature
{
    internal readonly struct SignatureWriter
        <TRollingHashAlgorithm, TStrongHashAlgorithm> : IDisposable
        where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
        where TStrongHashAlgorithm : IStrongHashAlgorithm
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private readonly SignatureOptions _options;
        private readonly TRollingHashAlgorithm _rollingHashAlgorithm;
        private readonly TStrongHashAlgorithm _strongHashAlgorithm;
        private readonly IMemoryOwner<byte> _strongHashBuffer;
        private const uint _flushThreshhold = 1 << 12;

        public SignatureWriter(
            PipeReader reader,
            PipeWriter writer,
            SignatureOptions options,
            TRollingHashAlgorithm rollingHashAlgorithm,
            TStrongHashAlgorithm strongHashAlgorithm,
            MemoryPool<byte> memoryPool)
        {
            _reader = reader;
            _writer = writer;
            _options = options;
            _rollingHashAlgorithm = rollingHashAlgorithm;
            _strongHashAlgorithm = strongHashAlgorithm;
            _strongHashBuffer = memoryPool.Rent(_options.StrongHashLength);
        }

        public void Dispose()
        {
            _strongHashBuffer.Dispose();
            _strongHashAlgorithm.Dispose();
        }

        public async ValueTask Write(CancellationToken ct)
        {
            _writer.Write(new SignatureHeader(_options));
            await WriteBlockSignatures(ct).ConfigureAwait(false);
            await _writer.FlushAsync(ct).ConfigureAwait(false);
            _reader.Complete();
            _writer.Complete();
        }

        private async ValueTask WriteBlockSignatures(CancellationToken ct)
        {
            FlushResult flushResult = default;
            int writtenSinceFlush = new SignatureHeader().Size;
            while (!flushResult.IsCompleted)
            {
                var readResult = await _reader.Buffer(_options.BlockLength, ct).ConfigureAwait(false);
                if (readResult.Buffer.IsEmpty)
                {
                    return;
                }
                var sig = ComputeSignature(readResult.Buffer);
                _reader.AdvanceTo(readResult.Buffer.End);
                writtenSinceFlush += _writer.Write(sig, _options);
                if (writtenSinceFlush >= _flushThreshhold)
                {
                    flushResult = await _writer.FlushAsync(ct).ConfigureAwait(false);
                    writtenSinceFlush = 0;
                }
            }
        }

        private BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm> ComputeSignature
            (in ReadOnlySequence<byte> block)
        {
            Debug.Assert(block.Length <= _options.BlockLength);

            var rollingHashAlgorithm = _rollingHashAlgorithm;
            var rollingHash = rollingHashAlgorithm.RotateIn(block);

            var strongHash = _strongHashBuffer.Memory
                .Slice(0, _options.StrongHashLength)
                .Span;
            _strongHashAlgorithm.Hash(block, strongHash);

            return new BlockSignature<TRollingHashAlgorithm, TStrongHashAlgorithm>
                (rollingHash, strongHash);
        }
    }
}
