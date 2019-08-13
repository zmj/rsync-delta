using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ushort _blockSigLength;
        private const uint _flushThreshhold = 2 << 13;

        public SignatureWriter(
            PipeReader reader,
            PipeWriter writer,
            SignatureOptions options,
            MemoryPool<byte> memoryPool)
        {
            _reader = reader;
            _writer = writer;
            _options = options;
            _blockSigLength = BlockSignature.Size((ushort)options.StrongHashLength);

            _blake2b = new Blake2b(memoryPool);
            _strongHash = memoryPool.Rent((int)_options.StrongHashLength);
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
            header.WriteTo(_writer.GetSpan(header.Size));
            _writer.Advance(header.Size);
        }

        private async ValueTask WriteBlockSignatures(CancellationToken ct)
        {
            int writtenSinceFlush = new SignatureHeader().Size;
            while (true)
            {
                var readResult = await _reader.Buffer(_options.BlockLength, ct);
                if (readResult.Buffer.IsEmpty)
                {
                    return;
                }
                WriteBlockSignature(readResult.Buffer);
                _reader.AdvanceTo(readResult.Buffer.End);

                writtenSinceFlush += _blockSigLength;
                if (writtenSinceFlush >= _flushThreshhold)
                {
                    await _writer.FlushAsync(ct); // handle flushresult
                    writtenSinceFlush = 0;
                }
            }
        }

        private void WriteBlockSignature(ReadOnlySequence<byte> block)
        {
            Debug.Assert(block.Length <= _options.BlockLength);
            var rollingHash = new RollingHash();
            rollingHash.RotateIn(block);

            var strongHash = _strongHash.Memory
                .Slice(0, (int)_options.StrongHashLength);
            _blake2b.Hash(block, strongHash.Span);
            
            var sig = new BlockSignature(
                rollingHash.Value,
                strongHash);
            sig.WriteTo(_writer.GetSpan(_blockSigLength));
            _writer.Advance(_blockSigLength);
        }
    }
}