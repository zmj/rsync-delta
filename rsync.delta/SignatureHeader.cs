using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Rsync.Delta
{
    internal readonly struct SignatureHeader
    {
        public const ushort Size = 4 + SignatureOptions.Size;

        public readonly SignatureFormat Format;
        public readonly SignatureOptions Options;

        public SignatureHeader(SignatureOptions options)
        {
            Format = SignatureFormat.Blake2b;
            Options = options;
        }

        public SignatureHeader(ref ReadOnlySequence<byte> buffer)
        {
            Format = (SignatureFormat)buffer.ReadUIntBigEndian();
            Options = new SignatureOptions(ref buffer);
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