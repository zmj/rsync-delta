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
            cmd.ArgumentList.Add("--force");
            cmd.ArgumentList.Add("signature");
            cmd.ArgumentList.Add("-R"); 
            cmd.ArgumentList.Add("rollsum"); // take sigoptions input
            cmd.ArgumentList.Add("-H");
            cmd.ArgumentList.Add("blake2");
            if (blockLength != null)
            {
                cmd.ArgumentList.Add("-b");
                cmd.ArgumentList.Add(blockLength.ToString());
            }
            if (strongHashLength != null)
            {
                cmd.ArgumentList.Add("-S");
                cmd.ArgumentList.Add(strongHashLength.ToString());
            }
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