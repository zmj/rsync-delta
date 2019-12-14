﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Rsync.Delta;

namespace Rsync.Delta.Benchmarks
{
    [MemoryDiagnoser]
    public class SignatureBenchmark
    {
        private IRdiff _rdiff;
        private byte[] _content;

        [Params(1 << 20)]
        public long Length;

        [GlobalSetup]
        public void Setup()
        {
            _rdiff = new Rdiff();
            _content = new byte[Length];
            new Random(5).NextBytes(_content);
        }

        [Benchmark]
        public async Task Signature()
        {
            await _rdiff.Signature(
                new MemoryStream(_content),
                new NullStream());
        }
    }
}
