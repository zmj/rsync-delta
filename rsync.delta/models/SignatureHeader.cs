using System;
using System.Buffers;
using System.Buffers.Binary;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Models
{
    internal readonly struct SignatureHeader : IWritable
    {
        public int Size => 4 + Options.Size;

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
            Options = new SignatureOptions(ref buffer);
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
            Options.WriteTo(buffer.Slice(4));
        }
    }

    internal enum SignatureFormat : uint
    {
        Blake2b = 0x72730137,
    }
}