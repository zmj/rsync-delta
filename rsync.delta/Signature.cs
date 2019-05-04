using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public class Signature
    {
        public async Task Generate(PipeReader reader, PipeWriter writer)
        {
            if (reader == null) { throw new ArgumentNullException(nameof(PipeReader)); }
            if (writer == null) { throw new ArgumentNullException(nameof(PipeWriter)); }

            WriteFileHeader(writer);
            await writer.FlushAsync(); // how often to flush?

            while (true)
            {
                ReadResult result;
                if (!reader.TryRead(out result))
                {
                    result = await reader.ReadAsync();
                }
                WriteBlocks(result.Buffer, writer);
                await writer.FlushAsync();
                reader.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }

        private void WriteFileHeader(PipeWriter writer)
        {
            var buffer = writer.GetSpan(sizeHint: 12);

            var format = SignatureFormat.Blake2b;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)format);
            buffer = buffer.Slice(start: 4);

            uint bytesPerBlock = 1;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, bytesPerBlock);
            buffer = buffer.Slice(start: 4);
            
            uint bytesPerStrongSum = 32;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, bytesPerStrongSum);

            writer.Advance(bytes: 12);
        }

        private void WriteBlocks(ReadOnlySequence<byte> buffer, PipeWriter writer)
        {
            while (buffer.Length > 0)
            {
                var span = buffer.First.Span;
                for (int i=0; i<span.Length; i++)
                {
                    WriteBlock(span.Slice(i, 1), writer);
                }
                buffer = buffer.Slice(span.Length);
            }
        }

        private void WriteBlock(ReadOnlySpan<byte> block, PipeWriter writer)
        {
            Console.WriteLine($"b: {BitConverter.ToString(block.ToArray())}");
            var hash = new RollingHash();
            hash.Hash(block);

            var buffer = writer.GetSpan(sizeHint: 4);
            hash.WriteTo(buffer);
            Console.WriteLine($"h: {BitConverter.ToString(buffer.Slice(0, 4).ToArray())}");
            writer.Advance(bytes: 4);

            // write blake2 hash
        }
    }

    internal enum SignatureFormat : uint
    {
        Blake2b = 0x72730137,
    }
}
