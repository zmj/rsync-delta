using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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