using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    internal readonly struct SignatureWriter
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private readonly SignatureOptions _options;

        public SignatureWriter(
            PipeReader reader,
            PipeWriter writer,
            SignatureOptions options)
        {
            _reader = reader;
            _writer = writer;
            _options = options;
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
            await foreach (var buffer in ReadBlocks(ct))
            {
                WriteBlockSignature(buffer);
            }
        }

        private async IAsyncEnumerable<ReadOnlySequence<byte>> ReadBlocks(
            CancellationToken ct)
        {
            while (true)
            {
                var readResult = await _reader.Buffer(_options.BlockLength, ct);
                if (readResult.Buffer.IsEmpty)
                {
                    yield break;
                }
                yield return readResult.Buffer;
                _reader.AdvanceTo(readResult.Buffer.End);
            }
        }

        private void WriteBlockSignature(ReadOnlySequence<byte> block)
        {
            Debug.Assert(block.Length <= _options.BlockLength);
            // todo: reuse a hash instance, but still hash each block independently
            // also don't copy here
            var buffer = block.ToArray();

            var rollingHash = new RollingHash();
            rollingHash.Hash(buffer);
            var strongHash = Blake2.Blake2b.Hash(buffer);
            
            var sig = new BlockSignature(rollingHash.Value, strongHash.AsMemory());
            int size = BlockSignature.Size((ushort)strongHash.Length);
            sig.WriteTo(_writer.GetSpan(size));
            _writer.Advance(size);
        }
    }
}