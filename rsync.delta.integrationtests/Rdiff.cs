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
            int? blockLength = null,
            int? strongHashLength = null)
        {
            var cmd = new ProcessStartInfo("rdiff");
            cmd.ArgumentList.Add("signature");
            if (blockLength != null)
            {
                cmd.ArgumentList.Add($"-b {blockLength}");
            }
            if (strongHashLength != null)
            {
                cmd.ArgumentList.Add($"-S {strongHashLength}");
            }
            cmd.ArgumentList.Add(_dir.Path(v1));
            cmd.ArgumentList.Add(_dir.Path(sig));
            Execute(cmd);
        }

        public void Delta(TestFile sig, TestFile v2, TestFile delta)
        {
            var cmd = new ProcessStartInfo("rdiff");
            cmd.ArgumentList.Add("delta");
            cmd.ArgumentList.Add(_dir.Path(sig));
            cmd.ArgumentList.Add(_dir.Path(v2));
            cmd.ArgumentList.Add(_dir.Path(delta));
            Execute(cmd);
        }

        private void Execute(ProcessStartInfo cmd)
        {
            using var process = new Process { StartInfo = cmd };
            bool ok = process.Start();
            if (!ok)
            {
                throw new Exception("process failed to start");
            }
            process.WaitForExit();
        }
    }
}