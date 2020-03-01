using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Rsync.Delta.Benchmarks
{
    [MemoryDiagnoser]
    public class DeltaBenchmark
    {
        private IRdiff _rdiff;
        private byte[] _v1;
        private byte[] _v2;
        private byte[] _signature;

        [Params(1 << 26)]
        public long Length;

        [Params(true, false)]
        public bool ChangeAll;

        [GlobalSetup]
        public async Task Setup()
        {
            _rdiff = new Rdiff();
            _v1 = new byte[Length];
            new Random(5).NextBytes(_v1);
            _v2 = new byte[Length];
            for (int i = 0; i < Length; i++) { _v2[i] = (byte)(_v1[i] + 1); }

            var sig = new MemoryStream();
            await _rdiff.Signature(new MemoryStream(_v1), sig);
            _signature = sig.ToArray();
        }

        [Benchmark]
        public async Task Delta()
        {
            await (ChangeAll ? AllChange() : NoChange());
        }

        private async Task NoChange()
        {
            await _rdiff.Delta(
                new MemoryStream(_signature),
                new MemoryStream(_v1),
                new NullStream());
        }

        private async Task AllChange()
        {
            await _rdiff.Delta(
                new MemoryStream(_signature),
                new MemoryStream(_v2),
                new NullStream());
        }
    }
}
