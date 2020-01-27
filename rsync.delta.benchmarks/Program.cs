using System;
using BenchmarkDotNet.Running;

namespace Rsync.Delta.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SignatureBenchmark>();
            Console.WriteLine(summary);
        }
    }
}
