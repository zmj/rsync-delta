using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public class Delta2
    {
        private const uint _deltaMagic = 0x72730236;

        public async Task Generate(
            PipeReader signatureReader,
            PipeReader fileReader,
            PipeWriter deltaWriter,
            CancellationToken ct = default)
        {
            SignatureHeader header = await ReadSignatureHeader(signatureReader, ct);
            WriteDeltaHeader(deltaWriter);
            await deltaWriter.FlushAsync(ct); // flush freq?

            // 1-byte format:
            // copy 0,3: 'hel'
            // copy 2,1: 'l'
            // copy 4,1: 'o'
            // copy 4,1: 'o'
            // copy 4,1: 'o'
        }

        private async ValueTask<SignatureHeader> ReadSignatureHeader(
            PipeReader reader, 
            CancellationToken ct)
        {
            ReadResult result;
            while (true)
            {
                if (!reader.TryRead(out result))
                {
                    result = await reader.ReadAsync(ct);
                }
                if (result.Buffer.Length >= SignatureHeader.Size)
                {
                    break;
                }
                if (result.IsCompleted)
                {
                    throw new Exception($"Invalid signature format: expected at least {SignatureHeader.Size} bytes, received {result.Buffer.Length} bytes");
                }
                reader.AdvanceTo(
                    consumed: result.Buffer.Start,
                    examined: result.Buffer.End);
            }
            if (!result.Buffer.IsSingleSegment) { throw new NotImplementedException(); }
            if (result.Buffer.First.Length < SignatureHeader.Size) { throw new NotImplementedException(); }
            var header = new SignatureHeader(result.Buffer.First.Span);
            reader.AdvanceTo(result.Buffer.GetPosition(SignatureHeader.Size));
            return header;
        }

        private async IAsyncEnumerable<BlockSignature> ReadBlockSignatures(
            SignatureHeader header,
            PipeReader reader,
            CancellationToken ct)
        {
            while (true)
            {
                BlockSignature? sig = await ReadBlockSignature(
                    header, reader, ct);
                if (!sig.HasValue) { break; }
                yield return sig.Value;
            }
        }

        private async ValueTask<BlockSignature?> ReadBlockSignature(
            SignatureHeader header,
            PipeReader reader,
            CancellationToken ct)
        {
            uint size = BlockSignature.Size(header.StrongHashLength);
            ReadResult readResult;
            while (true)
            {
                if (!reader.TryRead(out readResult))
                {
                    readResult = await reader.ReadAsync(ct);
                }
                if (readResult.Buffer.Length >= SignatureHeader.Size)
                {
                    break;
                }
                if (readResult.IsCompleted)
                {
                    throw new Exception($"Invalid block: expected at least {SignatureHeader.Size} bytes, received {readResult.Buffer.Length} bytes");
                }
                reader.AdvanceTo(
                    consumed: readResult.Buffer.Start,
                    examined: readResult.Buffer.End);
            }
            if (!readResult.Buffer.IsSingleSegment) { throw new NotImplementedException(); }
            if (readResult.Buffer.First.Length < size) { throw new NotImplementedException(); }
            var blockSig = new BlockSignature(readResult.Buffer.FirstSpan, header.StrongHashLength);
            reader.AdvanceTo(readResult.Buffer.GetPosition(size));
            return blockSig;
        }

        private void WriteDeltaHeader(PipeWriter writer)
        {
            Span<byte> buffer = writer.GetSpan(4);
            BinaryPrimitives.WriteUInt32BigEndian(buffer, _deltaMagic);
            writer.Advance(4);
        }
    }

    internal enum DeltaFormat : uint
    {
        Default = 0x72730236,
    }
}