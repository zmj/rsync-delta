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

            await foreach (var sig in ReadBlockSignatures(header, signatureReader, ct))
            {
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
            var header = default(SignatureHeader); // new SignatureHeader(result.Buffer.First.Span);
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
            uint size = BlockSignature.Size(header.Options.StrongHashLength);
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
            var blockSig = new BlockSignature(
                new SequenceReader<byte>(readResult.Buffer), 
                header.Options.StrongHashLength);
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
            LongRange? currentMatch = null;
            while (true)
            {
                ReadResult readResult = await reader.Buffer(header.Options.BlockLength, ct);
                if (readResult.IsCompleted && readResult.Buffer.Length == 0)
                {
                    if (currentMatch.HasValue)
                    {
                        WriteCopyCommand(writer, currentMatch.Value);
                    }
                    Console.WriteLine("flush writes");
                    await writer.FlushAsync(ct); // meh
                    return;
                }
                ReadOnlySequence<byte> buffer = readResult.Buffer;
                if (buffer.Length > header.Options.BlockLength)
                {
                    buffer = buffer.Slice(0, header.Options.BlockLength);
                }
                LongRange? matched = MatchBlock(header, buffer);
                if (!matched.HasValue) throw new NotImplementedException();
                reader.AdvanceTo(buffer.End);
                // todo use sequencereader for all reads
                if (matched.Value.TryAppendTo(ref currentMatch))
                {
                    continue;
                }
                else
                {
                    if (currentMatch.HasValue)
                    {
                        Console.WriteLine($"write: {currentMatch.Value.Start},{currentMatch.Value.Length}");
                        WriteCopyCommand(writer, currentMatch.Value);
                    }
                    currentMatch = matched.Value;
                }
            }
        }

        private LongRange? MatchBlock(
            SignatureHeader header,
            ReadOnlySequence<byte> buffers)
        {
            if (!buffers.IsSingleSegment) throw new NotImplementedException();
            // todo: rolling hash optimization
            var buffer = buffers.FirstSpan;
            byte[] hash = Blake2.Blake2b.Hash(buffer);
            for (int i=0; i<_blockSignatures.Count; i++)
            {
                var sig = _blockSignatures[i];
                if (hash.SequenceEqual(sig.StrongHash.ToArray()))
                {
                    return new LongRange(
                        start: (uint)i * (ulong)header.Options.BlockLength,
                        length: header.Options.BlockLength);
                }
            }
            return null;
        }

        private void WriteCopyCommand(PipeWriter writer, LongRange range)
        {
            var command = new CopyCommand(range);
            var buffer = writer.GetSpan(command.Size);
            command.WriteTo(buffer);
            writer.Advance(command.Size);
            Console.WriteLine($"advance: {command.Size}");
        }
    }

    internal enum DeltaFormat : uint
    {
        Default = 0x72730236,
    }
}