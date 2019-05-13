using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public class Signature
    {
        public async Task Generate(PipeReader reader, PipeWriter writer, int? blockSize)
        {
            if (reader == null) { throw new ArgumentNullException(nameof(PipeReader)); }
            if (writer == null) { throw new ArgumentNullException(nameof(PipeWriter)); }
            blockSize ??= 2048;

            WriteFileHeader(writer, blockSize.Value);
            await writer.FlushAsync(); // how often to flush?

            while (true)
            {
                ReadResult result;
                if (!reader.TryRead(out result))
                {
                    result = await reader.ReadAsync();
                }
                WriteBlocks(result.Buffer, writer, blockSize.Value);
                await writer.FlushAsync();
                reader.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
            writer.Complete();
        }

        private void WriteFileHeader(PipeWriter writer, int blockSize)
        {
            // todo: fix all the casts
            var header = new SignatureHeader(
                blockLength: (uint)blockSize, 
                strongHashLength: 32);
            var buffer = writer.GetSpan((int)SignatureHeader.Size);
            header.WriteTo(buffer);
            writer.Advance((int)SignatureHeader.Size);
        }

        private void WriteBlocks(ReadOnlySequence<byte> buffer, PipeWriter writer, int blockSize)
        {
            while (buffer.Length > 0)
            {
                var span = buffer.First.Span;
                for (int i=0; i<span.Length; i+= blockSize)
                {
                    int end = (i + blockSize > span.Length) ? (span.Length-i) : blockSize;
                    WriteBlock(span.Slice(i, end), writer);
                }
                // this doesn't consume entire span
                buffer = buffer.Slice(span.Length);
            }
        }

        private void WriteBlock(ReadOnlySpan<byte> block, PipeWriter writer)
        {
            var hash = new RollingHash();
            hash.Hash(block);

            var buffer = writer.GetSpan(sizeHint: 4);
            uint hashV = hash.Value;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, hashV);
            writer.Advance(bytes: 4);

            buffer = writer.GetSpan(sizeHint: 32).Slice(0, 32);
            ReadOnlySpan<byte> hash2 = Blake2.Blake2b.Hash(block);
            hash2.CopyTo(buffer);
            writer.Advance(buffer.Length);
        }
    }

    internal enum SignatureFormat : uint
    {
        Blake2b = 0x72730137,
    }
}
