using System;
using System.Collections.Generic;
using System.Text;

namespace Rsync.Delta.UnitTests
{
    internal class TestCase
    {
        public readonly byte[] Version1;
        public readonly byte[] Version2;
        private readonly byte[] _sigAdler;
        private readonly byte[] _sigRabinKarp;
        public readonly byte[] Delta;
        public readonly int BlockLength;
        public readonly int StrongHashLength;

        private TestCase(
            string version1,
            string version2,
            string sigAdler,
            string sigRabinKarp,
            string delta,
            int? blockLength = null,
            int? strongHashLength = null)
        {
            Version1 = Encoding.UTF8.GetBytes(version1);
            Version2 = Encoding.UTF8.GetBytes(version2);
            _sigAdler = GetBytes(sigAdler);
            _sigRabinKarp = GetBytes(sigRabinKarp);
            Delta = GetBytes(delta);
            BlockLength = blockLength ?? default(SignatureOptions).BlockLength;
            StrongHashLength = strongHashLength ?? default(SignatureOptions).StrongHashLength;
        }

        private static byte[] GetBytes(string hex)
        {
            hex = hex
                .Replace("-", "")
                .Replace("_", "")
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace("\r", "");
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
            return bytes;
        }

        public static IEnumerable<object[]> HashAlgorithms()
        {
            yield return new object[]
            {
                RollingHashAlgorithm.Adler,
                StrongHashAlgorithm.Blake2b,
            };
            yield return new object[]
            {
                RollingHashAlgorithm.RabinKarp,
                StrongHashAlgorithm.Blake2b,
            };
            /*yield return new object[]
            {
                RollingHashAlgorithm.Adler,
                StrongHashAlgorithm.Md4,
            };
            yield return new object[]
            {
                RollingHashAlgorithm.RabinKarp,
                StrongHashAlgorithm.Md4,
            };*/
        }

        public byte[] Sig(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            rollingHash switch
            {
                RollingHashAlgorithm.Adler => _sigAdler,
                RollingHashAlgorithm.RabinKarp => _sigRabinKarp,
                _ => throw new NotImplementedException(),
            };

        public static readonly TestCase Hello_Hellooo_Default = new TestCase(
            version1: "hello",
            version2: "hellooo",
            sigAdler: @"7273 0137 0000 0800 0000 0020 07f8 02af
                        324d cf02 7dd4 a30a 932c 441f 365a 25e8
                        6b17 3def a4b8 e589 4825 3471 b81b 72cf",
            sigRabinKarp: @"7273 0147 0000 0800 0000 0020 c918 3e85
                            324d cf02 7dd4 a30a 932c 441f 365a 25e8
                            6b17 3def a4b8 e589 4825 3471 b81b 72cf",
            delta: "7273 0236 0768 656c 6c6f 6f6f 00");

        public static readonly TestCase Hello_Hellooo_BlockLength_1 = new TestCase(
            version1: "hello",
            version2: "hellooo",
            sigAdler: @"7273 0137 0000 0001 0000 0020 0087 0087
                        23c7 46ec c949 815d 3a1f 142c f32a 29b8
                        04d8 9274 e792 fcd5 66c6 59b7 e4e5 f3bd
                        0084 0084 8d23 4302 aeb0 6f2a 7eff b905
                        e43f 037e 4dca 1c2a 0f05 0e82 1753 28c5
                        ce8d 31f4 008b 008b 5b27 9c19 ca59 4f9b
                        2ef0 329f 2981 29cf d4b7 147f f27a 6486
                        b144 10e5 cc50 b1a6 008b 008b 5b27 9c19
                        ca59 4f9b 2ef0 329f 2981 29cf d4b7 147f
                        f27a 6486 b144 10e5 cc50 b1a6 008e 008e
                        deda b4ca 7101 07c6 6d0f 9bad 9aef 7f52
                        f995 8e72 4198 fb99 81b7 7e3f b6ae 4e56",
            sigRabinKarp: @"7273 0147 0000 0001 0000 0020 0810 428d
                            23c7 46ec c949 815d 3a1f 142c f32a 29b8
                            04d8 9274 e792 fcd5 66c6 59b7 e4e5 f3bd
                            0810 428a 8d23 4302 aeb0 6f2a 7eff b905
                            e43f 037e 4dca 1c2a 0f05 0e82 1753 28c5
                            ce8d 31f4 0810 4291 5b27 9c19 ca59 4f9b
                            2ef0 329f 2981 29cf d4b7 147f f27a 6486
                            b144 10e5 cc50 b1a6 0810 4291 5b27 9c19
                            ca59 4f9b 2ef0 329f 2981 29cf d4b7 147f
                            f27a 6486 b144 10e5 cc50 b1a6 0810 4294
                            deda b4ca 7101 07c6 6d0f 9bad 9aef 7f52
                            f995 8e72 4198 fb99 81b7 7e3f b6ae 4e56",
            delta: @"7273 0236 4500 0345 0201 4504 0145 0401
                     4504 0100",
            blockLength: 1);

        public static readonly TestCase Hello_Hellooo_BlockLength_2 = new TestCase(
            version1: "hello",
            version2: "hellooo",
            sigAdler: @"7273 0137 0000 0002 0000 0020 0192 010b
                        734f 36ae 49af 5392 ff75 f37e 3583 6da9
                        0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                        01a1 0116 e3b9 cf41 f998 57e0 ab72 5a32
                        8580 330d 45dd 35af 9f61 fed0 c38a bab7
                        dc55 0748 008e 008e deda b4ca 7101 07c6
                        6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                        81b7 7e3f b6ae 4e56",
            sigRabinKarp: @"7273 0147 0000 0002 0000 0020 ec51 f8c6
                            734f 36ae 49af 5392 ff75 f37e 3583 6da9
                            0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                            0c93 0161 e3b9 cf41 f998 57e0 ab72 5a32
                            8580 330d 45dd 35af 9f61 fed0 c38a bab7
                            dc55 0748 0810 4294 deda b4ca 7101 07c6
                            6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                            81b7 7e3f b6ae 4e56",
            delta: "7273 0236 4500 0402 6f6f 4504 0100",
            blockLength: 2);

        public static readonly TestCase Hello_Hellooo_StrongHashLength_15 = new TestCase(
            version1: "hello",
            version2: "hellooo",
            sigAdler: @"7273 0137 0000 0800 0000 000f 07f8 02af
                        324d cf02 7dd4 a30a 932c 441f 365a 25",
            sigRabinKarp: @"7273 0147 0000 0800 0000 000f c918 3e85
                            324d cf02 7dd4 a30a 932c 441f 365a 25",
            delta: "7273 0236 0768 656c 6c6f 6f6f 00",
            strongHashLength: 15);

        public static readonly TestCase Hello_Hellooo_StrongHashLength_16 = new TestCase(
            version1: "hello",
            version2: "hellooo",
            sigAdler: @"7273 0137 0000 0800 0000 0010 07f8 02af
                        324d cf02 7dd4 a30a 932c 441f 365a 25e8",
            sigRabinKarp: @"7273 0147 0000 0800 0000 0010 c918 3e85
                            324d cf02 7dd4 a30a 932c 441f 365a 25e8",
            delta: "7273 0236 0768 656c 6c6f 6f6f 00",
            strongHashLength: 16);

        public static readonly TestCase Hello_Hellooo_StrongHashLength_17 = new TestCase(
            version1: "hello",
            version2: "hellooo",
            sigAdler: @"7273 0137 0000 0800 0000 0011 07f8 02af
                        324d cf02 7dd4 a30a 932c 441f 365a 25e8
                        6b",
            sigRabinKarp: @"7273 0147 0000 0800 0000 0011 c918 3e85
                            324d cf02 7dd4 a30a 932c 441f 365a 25e8
                            6b",
            delta: "7273 0236 0768 656c 6c6f 6f6f 00",
            strongHashLength: 17);

        public static readonly TestCase Hello_Hello_BlockLength_2 = new TestCase(
            version1: "hello",
            version2: "hello",
            sigAdler: @"7273 0137 0000 0002 0000 0020 0192 010b
                        734f 36ae 49af 5392 ff75 f37e 3583 6da9
                        0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                        01a1 0116 e3b9 cf41 f998 57e0 ab72 5a32
                        8580 330d 45dd 35af 9f61 fed0 c38a bab7
                        dc55 0748 008e 008e deda b4ca 7101 07c6
                        6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                        81b7 7e3f b6ae 4e56",
            sigRabinKarp: @"7273 0147 0000 0002 0000 0020 ec51 f8c6
                            734f 36ae 49af 5392 ff75 f37e 3583 6da9
                            0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                            0c93 0161 e3b9 cf41 f998 57e0 ab72 5a32
                            8580 330d 45dd 35af 9f61 fed0 c38a bab7
                            dc55 0748 0810 4294 deda b4ca 7101 07c6
                            6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                            81b7 7e3f b6ae 4e56",
            delta: "7273 0236 4500 0500",
            blockLength: 2);

        public static readonly TestCase Hello_Ohello_BlockLength_2 = new TestCase(
            version1: "hello",
            version2: "ohello",
            sigAdler: @"7273 0137 0000 0002 0000 0020 0192 010b
                        734f 36ae 49af 5392 ff75 f37e 3583 6da9
                        0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                        01a1 0116 e3b9 cf41 f998 57e0 ab72 5a32
                        8580 330d 45dd 35af 9f61 fed0 c38a bab7
                        dc55 0748 008e 008e deda b4ca 7101 07c6
                        6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                        81b7 7e3f b6ae 4e56",
            sigRabinKarp: @"7273 0147 0000 0002 0000 0020 ec51 f8c6
                            734f 36ae 49af 5392 ff75 f37e 3583 6da9
                            0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                            0c93 0161 e3b9 cf41 f998 57e0 ab72 5a32
                            8580 330d 45dd 35af 9f61 fed0 c38a bab7
                            dc55 0748 0810 4294 deda b4ca 7101 07c6
                            6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                            81b7 7e3f b6ae 4e56",
            delta: "7273 0236 016f 4500 0500",
            blockLength: 2);

        public static readonly TestCase Hello_Ohhello_BlockLength_2 = new TestCase(
            version1: "hello",
            version2: "ohhello",
            sigAdler: @"7273 0137 0000 0002 0000 0020 0192 010b
                        734f 36ae 49af 5392 ff75 f37e 3583 6da9
                        0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                        01a1 0116 e3b9 cf41 f998 57e0 ab72 5a32
                        8580 330d 45dd 35af 9f61 fed0 c38a bab7
                        dc55 0748 008e 008e deda b4ca 7101 07c6
                        6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                        81b7 7e3f b6ae 4e56",
            sigRabinKarp: @"7273 0147 0000 0002 0000 0020 ec51 f8c6
                            734f 36ae 49af 5392 ff75 f37e 3583 6da9
                            0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                            0c93 0161 e3b9 cf41 f998 57e0 ab72 5a32
                            8580 330d 45dd 35af 9f61 fed0 c38a bab7
                            dc55 0748 0810 4294 deda b4ca 7101 07c6
                            6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                            81b7 7e3f b6ae 4e56",
            delta: "7273 0236 026f 6845 0005 00",
            blockLength: 2);

        public static readonly TestCase Hello_Heollo_BlockLength_2 = new TestCase(
            version1: "hello",
            version2: "heollo",
            sigAdler: @"7273 0137 0000 0002 0000 0020 0192 010b
                        734f 36ae 49af 5392 ff75 f37e 3583 6da9
                        0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                        01a1 0116 e3b9 cf41 f998 57e0 ab72 5a32
                        8580 330d 45dd 35af 9f61 fed0 c38a bab7
                        dc55 0748 008e 008e deda b4ca 7101 07c6
                        6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                        81b7 7e3f b6ae 4e56",
            sigRabinKarp: @"7273 0147 0000 0002 0000 0020 ec51 f8c6
                            734f 36ae 49af 5392 ff75 f37e 3583 6da9
                            0bc7 5047 7070 ccbb 3149 cdc1 2733 9214
                            0c93 0161 e3b9 cf41 f998 57e0 ab72 5a32
                            8580 330d 45dd 35af 9f61 fed0 c38a bab7
                            dc55 0748 0810 4294 deda b4ca 7101 07c6
                            6d0f 9bad 9aef 7f52 f995 8e72 4198 fb99
                            81b7 7e3f b6ae 4e56",
            delta: "7273 0236 4500 0201 6f45 0203 00",
            blockLength: 2);

        public static readonly TestCase LoremIpsum = new TestCase(
            version1: "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
            version2: "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
            sigAdler: @"7273 0137 0000 0800 0000 0020 86fb daeb
                        d6d9 034f 61e2 f7ad a6e5 8c25 2e15 684c
                        8df7 f0b1 97a9 5d80 f42c a0a3 685d e26e",
            sigRabinKarp: @"7273 0147 0000 0800 0000 0020 b9ef 8df5
                            d6d9 034f 61e2 f7ad a6e5 8c25 2e15 684c
                            8df7 f0b1 97a9 5d80 f42c a0a3 685d e26e",
            delta: "7273 0236 4600 01bd 00");
    }
}
