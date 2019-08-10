using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal readonly struct SignatureWriter : IDisposable
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private readonly SignatureOptions _options;
        private readonly IMemoryOwner<byte> _hashScratch;
        private readonly IMemoryOwner<byte> _strongHash;

        public SignatureWriter(
            PipeReader reader,
            PipeWriter writer,
            SignatureOptions options,
            MemoryPool<byte> memoryPool)
        {
            _reader = reader;
            _writer = writer;
            _options = options;
            
            _hashScratch = memoryPool.Rent(Blake2b.ScratchSize);
            _strongHash = memoryPool.Rent((int)_options.StrongHashLength);
        }

        public void Dispose()
        {
            _strongHash.Dispose();
            _hashScratch.Dispose();
        }

        public async ValueTask Write(CancellationToken ct)
        {
            try
            {
                WriteHeader();
                await WriteBlockSignatures(ct);
                await _writer.FlushAsync(ct); // handle flushresult
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

        private void WriteHeader()
        {
            var header = new SignatureHeader(_options);
            header.WriteTo(_writer.GetSpan(SignatureHeader.Size));
            _writer.Advance(SignatureHeader.Size);
        }

        private async ValueTask WriteBlockSignatures(CancellationToken ct)
        {
            while (true)
            {
                var readResult = await _reader.Buffer(_options.BlockLength, ct);
                if (readResult.Buffer.IsEmpty)
                {
                    return;
                }
                WriteBlockSignature(readResult.Buffer);
                _reader.AdvanceTo(readResult.Buffer.End);
            }
        }

        private void WriteBlockSignature(ReadOnlySequence<byte> block)
        {
            Debug.Assert(block.Length <= _options.BlockLength);
            var rollingHash = new RollingHash();
            rollingHash.RotateIn(block);

            var strongHash = _strongHash.Memory
                .Slice(0, (int)_options.StrongHashLength);
            new Blake2b(_hashScratch.Memory.Span, (byte)strongHash.Length)
                .Hash(block, strongHash.Span);
            
            var sig = new BlockSignature(
                rollingHash.Value,
                strongHash);
            int size = BlockSignature.Size((ushort)strongHash.Length);
            sig.WriteTo(_writer.GetSpan(size));
            _writer.Advance(size);
        }
    }
}