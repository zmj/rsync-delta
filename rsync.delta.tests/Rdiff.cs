using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Rsync.Delta.Tests
{
    internal readonly struct Rdiff
    {
        private readonly ProcessStartInfo _proc;

        private Rdiff(string firstArg)
        {
            _proc = new ProcessStartInfo();
            _proc.RedirectStandardInput = true;
            _proc.RedirectStandardOutput = true;
            _proc.FileName = "rdiff";
            _proc.ArgumentList.Add(firstArg);
        }

        public static Rdiff Signature(
            int? blockLength = null,
            int? strongHashLength = null) 
        {
            var rdiff = new Rdiff("signature");
            if (blockLength.HasValue)
                rdiff._proc.ArgumentList.Add($"-b {blockLength}");
            if (strongHashLength.HasValue)
                rdiff._proc.ArgumentList.Add($"-S {strongHashLength}");
            return rdiff;
        }

        public static Rdiff Delta(TempFile sigFile)
        {
            var rdiff = new Rdiff("delta");
            rdiff._proc.ArgumentList.Add(sigFile.Name);
            return rdiff;
        }

        public static Rdiff Patch => new Rdiff("patch");

        public async Task Execute(Stream input, Stream output)
        {
            using var proc = new Process { StartInfo = _proc };
            bool ok = proc.Start();
            if (!ok) throw new Exception("Failed to start rdiff");
            await input.CopyToAsync(proc.StandardInput.BaseStream);
            input.Close();
            proc.StandardInput.BaseStream.Close();
            await proc.StandardOutput.BaseStream.CopyToAsync(output);
            output.Close();
        }
    }
}