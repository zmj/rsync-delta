using System;
using System.Diagnostics;

namespace Rsync.Delta.IntegrationTests
{
    internal static class Extensions
    {
        public static void Execute(this ProcessStartInfo cmd)
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