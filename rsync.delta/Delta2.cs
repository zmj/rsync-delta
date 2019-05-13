using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public class Delta2
    {
        private const uint _deltaMagic = 0x72730236;

        private readonly List<BlockSignature> _blockSignatures = new List<BlockSignature>();

        public async ValueTask Generate(
            PipeReader signatureReader,
            PipeReader fileReader,
            PipeWriter deltaWriter,
            CancellationToken ct = default)
        {
            SignatureHeader header = await ReadSignatureHeader(signatureReader, ct);
            WriteDeltaHeader(deltaWriter);
            await deltaWriter.FlushAsync(ct); // flush freq?

            await foreach (var sig in ReadBlockSignatures(header, signatureReader, ct))
            {
                Console.WriteLine($"Block: {sig.RollingHash.ToString("X")} {BitConverter.ToString(sig.StrongHash)}");
                _blockSignatures.Add(sig);
            }
            await WriteCommands(header, fileReader, deltaWriter, ct);
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
                    if (readResult.Buffer.Length != 0)
                    {
                        throw new Exception($"Invalid block: expected at least {SignatureHeader.Size} bytes, received {readResult.Buffer.Length} bytes");
                    }
                    return null;
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

        private async ValueTask WriteCommands(
            SignatureHeader header,
            PipeReader reader,
            PipeWriter writer,
            CancellationToken ct)
        {
            while (true)
            {
                ReadResult readResult = await reader.Buffer(header.BlockLength, ct);
                Console.WriteLine($"r: {readResult.Buffer.Length} {readResult.IsCompleted}");
                if (readResult.IsCompleted && readResult.Buffer.Length == 0)
                {
                    return;
                }
                LongRange? matched = MatchBlock(header, readResult.Buffer);
                if (!matched.HasValue) throw new NotImplementedException();
                Console.WriteLine($"match: {matched.Value.Start},{matched.Value.Length}");
                reader.AdvanceTo(readResult.Buffer.GetPosition(header.BlockLength));
            }
        }

        private LongRange? MatchBlock(
            SignatureHeader header,
            ReadOnlySequence<byte> buffers)
        {
            if (!buffers.IsSingleSegment) throw new NotImplementedException();
            // todo: rolling hash optimization
            var buffer = buffers.FirstSpan;
            if (buffer.Length > header.BlockLength)
            {
                buffer = buffer.Slice(0, (int)header.BlockLength);
            }
            byte[] hash = Blake2.Blake2b.Hash(buffer);
            for (int i=0; i<_blockSignatures.Count; i++)
            {
                var sig = _blockSignatures[i];
                if (hash.SequenceEqual(sig.StrongHash))
                {
                    return new LongRange(
                        start: (uint)i * (ulong)header.BlockLength,
                        length: header.BlockLength);
                }
            }
            return null;
        }
    }

    internal enum DeltaFormat : uint
    {
        Default = 0x72730236,
    }
}