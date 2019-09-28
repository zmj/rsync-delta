using System;
using System.Buffers;
using System.Buffers.Binary;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct SignatureHeader :
        IWritable, IReadable<SignatureHeader>
    {
        public int Size => 12;
        public int MaxSize => 12;

        public readonly SignatureFormat Format;
        public readonly SignatureOptions Options;

        public SignatureHeader(SignatureOptions options)
        {
            Format = SignatureFormat.Blake2b;
            Options = options;
            Validate();
        }

        public SignatureHeader(ref ReadOnlySequence<byte> buffer)
        {
            Format = (SignatureFormat)buffer.ReadUIntBigEndian();
            Options = new SignatureOptions(
                blockLength: buffer.ReadIntBigEndian(),
                strongHashLength: buffer.ReadIntBigEndian());
            Validate();
        }

        private void Validate()
        {
            if (Format != SignatureFormat.Blake2b)
            {
                throw new FormatException($"Unexpected signature magic: {Format}");
            }
        }

        public void WriteTo(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Format);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), Options.BlockLength);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(8), Options.StrongHashLength);
        }

        public SignatureHeader? ReadFrom(ref ReadOnlySequence<byte> data)
        {
            return new SignatureHeader(ref data);
        }
    }

    internal enum SignatureFormat : uint
    {
        Blake2b = 0x72730137,
    }
}