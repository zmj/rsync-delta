using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class SignatureTests2
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello", null, null)]
        [InlineData("hello", 1, null)]
        [InlineData("hello", 2, null)]
        public async Task Signature(
            string text, int? blockLength, int? strongHashLength)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var rdiffOut = new MemoryStream();
            await Rdiff(new MemoryStream(bytes), rdiffOut, blockLength, strongHashLength);
            var expected = BitConverter.ToString(rdiffOut.ToArray());

            var options = new SignatureOptions(
                (uint?)blockLength ?? SignatureOptions.Default.BlockLength,
                (uint?)strongHashLength ?? SignatureOptions.Default.StrongHashLength);
            var libraryOut = new MemoryStream();
            await _rsync.GenerateSignature(new MemoryStream(bytes), libraryOut, options);
            var actual = BitConverter.ToString(libraryOut.ToArray());

            Assert.Equal(expected, actual);
        }

        private async Task Rdiff(
            Stream input, 
            Stream output, 
            int? blockLength, 
            int? strongHashLength)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "rdiff";
            proc.StartInfo.ArgumentList.Add("signature");
            if (blockLength.HasValue) 
                proc.StartInfo.ArgumentList.Add($"-b {blockLength.Value}");
            if (strongHashLength.HasValue)
                proc.StartInfo.ArgumentList.Add($"-s {strongHashLength.Value}");
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            await input.CopyToAsync(proc.StandardInput.BaseStream);
            proc.StandardInput.Close();
            await proc.StandardOutput.BaseStream.CopyToAsync(output);
        }
    }
}