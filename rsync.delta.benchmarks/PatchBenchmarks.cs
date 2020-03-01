using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Rsync.Delta.Benchmarks
{
    [MemoryDiagnoser]
    public class PatchBenchmarks
    {
        private IRdiff _rdiff;
        private byte[] _v1;
        private byte[] _deltaNoChange;
        private byte[] _deltaAllChange;

        [Params(1 << 20)]
        public long Length;

        [GlobalSetup]
        public async Task Setup()
        {
            _rdiff = new Rdiff();
            _v1 = new byte[Length];
            new Random(5).NextBytes(_v1);

            var sigStream = new MemoryStream();
            await _rdiff.SignatureAsync(
                new MemoryStream(_v1),
                sigStream);
            var sig = sigStream.ToArray();

            var deltaNoChange = new MemoryStream();
            await _rdiff.DeltaAsync(
                new MemoryStream(sig),
                new MemoryStream(_v1),
                deltaNoChange);
            _deltaNoChange = deltaNoChange.ToArray();

            var v2 = new byte[Length];
            for (int i = 0; i < Length; i++) { v2[i] = (byte)(_v1[i] + 1); }

            var deltaAllChange = new MemoryStream();
            await _rdiff.DeltaAsync(
                new MemoryStream(sig),
                new MemoryStream(v2),
                deltaAllChange);
            _deltaAllChange = deltaAllChange.ToArray();
        }

        [Benchmark]
        public async Task NoChange()
        {
            await _rdiff.PatchAsync(
                new MemoryStream(_v1),
                new MemoryStream(_deltaNoChange),
                new NullStream());
        }

        [Benchmark]
        public async Task AllChange()
        {
            await _rdiff.PatchAsync(
                new MemoryStream(_v1),
                new MemoryStream(_deltaAllChange),
                new NullStream());
        }
    }
}
