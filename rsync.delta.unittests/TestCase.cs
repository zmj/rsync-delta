using System;
using System.Collections.Generic;
using System.Text;
using Rsync.Delta.Models;

namespace Rsync.Delta.UnitTests
{
    internal class TestCase
    {
        public readonly byte[] Version1;
        public readonly byte[] Version2;
        public readonly SignatureOptions Options;
        public readonly ReadOnlyMemory<byte> Signature;
        public readonly ReadOnlyMemory<byte> Delta;

        private TestCase(
            string version1,
            string version2,
            SignatureOptions options,
            string signature,
            string delta)
        {
            Version1 = Encoding.UTF8.GetBytes(version1);
            Version2 = Encoding.UTF8.GetBytes(version2);
            Options = options;
            Signature = GetBytes(signature);
            Delta = GetBytes(delta);
        }

        private static ReadOnlyMemory<byte> GetBytes(string hex)
        {
            hex = hex
                .Replace("-", "")
                .Replace("_", "")
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\t", "");
            if (hex.Length % 2 != 0) 
            {
                throw new ArgumentException("hex string must have even number of digits");
            }
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                bytes[i] = byte.Parse(
                    hex.AsSpan().Slice(i * 2, 2),
                    System.Globalization.NumberStyles.HexNumber);
            }
            return bytes.AsMemory();
        }

        public static readonly TestCase Hello_Hellooo_Default = new TestCase(
            version1: "hello",
            version2: "hellooo",
            SignatureOptions.Default,
            signature: @"7273 0137 0000 0800 0000 0020 07f8 02af
                         324d cf02 7dd4 a30a 932c 441f 365a 25e8
                         6b17 3def a4b8 e589 4825 3471 b81b 72cf",
            delta: "7273 0236 4107 6865 6c6c 6f6f 6f00");
    }
}