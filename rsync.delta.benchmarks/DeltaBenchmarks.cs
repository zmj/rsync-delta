using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Rsync.Delta;

namespace Rsync.Delta.Benchmarks
{
    [MemoryDiagnoser]
    public class DeltaBenchmarks
    {
        private IRdiff _rdiff;
        private byte[] _v1;
        private byte[] _v2;
        private byte[] _signature;

        [Params(1 << 20)]
        public long Length;

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
        public async Task NoChange()
        {
            await _rdiff.Delta(
                new MemoryStream(_signature),
                new MemoryStream(_v1),
                new NullStream());
        }

        [Benchmark]
        public async Task AllChange()
        {
            await _rdiff.Delta(
                new MemoryStream(_signature),
                new MemoryStream(_v2),
                new NullStream());
        }
    }
}
