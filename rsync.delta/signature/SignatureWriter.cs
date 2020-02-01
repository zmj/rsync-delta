﻿using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Hash.Adler;
using Rsync.Delta.Hash.Blake2b;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Signature
{
    internal readonly struct SignatureWriter : IDisposable
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private readonly SignatureOptions _options;
        private readonly Blake2b _blake2b;
        private readonly IMemoryOwner<byte> _strongHash;
        private const uint _flushThreshhold = 1 << 12;

        public SignatureWriter(
            PipeReader reader,
            PipeWriter writer,
            SignatureOptions options,
            MemoryPool<byte> memoryPool)
        {
            _reader = reader;
            _writer = writer;
            _options = options;

            _blake2b = new Blake2b(memoryPool);
            _strongHash = memoryPool.Rent(_options.StrongHashLength);
        }

        public void Dispose()
        {
            _strongHash.Dispose();
            _blake2b.Dispose();
        }

        public async ValueTask Write(CancellationToken ct)
        {
            try
            {
                _writer.Write(new SignatureHeader(_options));
                await WriteBlockSignatures(ct).ConfigureAwait(false);
                await _writer.FlushAsync(ct).ConfigureAwait(false);
                _reader.Complete();
                _writer.Complete();
            }
            catch (Exception ex)
            {
                _reader.Complete(ex);
                _writer.Complete(ex);
                throw;
            }
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

        private BlockSignature ComputeSignature(in ReadOnlySequence<byte> block)
        {
            Debug.Assert(block.Length <= _options.BlockLength);
            var rollingHash = new RollingHash();
            rollingHash.RotateIn(block);

            var strongHash = _strongHash.Memory
                .Slice(0, _options.StrongHashLength)
                .Span;
            _blake2b.Hash(block, strongHash);

            return new BlockSignature(
                rollingHash.Value,
                strongHash);
        }
    }
}
