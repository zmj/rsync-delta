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

        public static async Task<TempFile> Signature(
            Stream input,
            int? blockLength = null,
            int? strongHashLength = null)
        {
            var rdiff = new Rdiff("signature");
            if (blockLength.HasValue)
                rdiff._proc.ArgumentList.Add($"-b {blockLength}");
            if (strongHashLength.HasValue)
                rdiff._proc.ArgumentList.Add($"-S {strongHashLength}");
            var sig = new TempFile();
            await rdiff.Execute(input, sig.Stream());
            return sig;
        }

        public static async Task<TempFile> Delta(TempFile sig, Stream input)
        {
            var rdiff = new Rdiff("delta");
            rdiff._proc.ArgumentList.Add(sig.Name);
            var delta = new TempFile();
            await rdiff.Execute(input, delta.Stream());
            return delta;
        }

        public static async Task<TempFile> Patch(TempFile basis, TempFile delta)
        {
            var rdiff = new Rdiff("patch");
            rdiff._proc.ArgumentList.Add(basis.Name);
            var patched = new TempFile();
            await rdiff.Execute(delta.Stream(), patched.Stream());
            return patched;
        }

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