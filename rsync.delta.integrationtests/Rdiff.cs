using System;
using System.Diagnostics;

namespace Rsync.Delta.IntegrationTests
{
    internal readonly struct Rdiff
    {
        private readonly TestDirectory _dir;

        public Rdiff(TestDirectory dir) => _dir = dir;

        public void Signature(
            TestFile v1,
            TestFile sig,
            SignatureOptions options)
        {
            var cmd = new ProcessStartInfo("rdiff");
            cmd.ArgumentList.Add("--force");
            cmd.ArgumentList.Add("signature");

            cmd.ArgumentList.Add("-R");
            cmd.ArgumentList.Add(options.RollingHash switch
            {
                RollingHashAlgorithm.RabinKarp => "rabinkarp",
                RollingHashAlgorithm.Adler => "rollsum",
                _ => throw new NotImplementedException(options.RollingHash.ToString())
            });

            cmd.ArgumentList.Add("-H");
            cmd.ArgumentList.Add(options.StrongHash switch
            {
                StrongHashAlgorithm.Blake2b => "blake2",
                _ => throw new NotImplementedException(options.StrongHash.ToString())
            });

            cmd.ArgumentList.Add("-b");
            cmd.ArgumentList.Add(options.BlockLength.ToString());

            cmd.ArgumentList.Add("-S");
            cmd.ArgumentList.Add(options.StrongHashLength.ToString());

            cmd.ArgumentList.Add(_dir.Path(v1));
            cmd.ArgumentList.Add(_dir.Path(sig));
            cmd.Execute();
        }

        public void Delta(TestFile sig, TestFile v2, TestFile delta)
        {
            var cmd = new ProcessStartInfo("rdiff");
            cmd.ArgumentList.Add("--force");
            cmd.ArgumentList.Add("delta");
            cmd.ArgumentList.Add(_dir.Path(sig));
            cmd.ArgumentList.Add(_dir.Path(v2));
            cmd.ArgumentList.Add(_dir.Path(delta));
            cmd.Execute();
        }
    }
}
