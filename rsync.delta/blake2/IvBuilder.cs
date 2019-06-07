using System;

#nullable disable
namespace Rsync.Delta.Blake2
{
    internal static class IvBuilder
    {
		public static void ConfigB(int outputSize, Span<ulong> rawConfig)
		{
			//digest length
			rawConfig[0] |= (ulong)(uint)outputSize;

			// FanOut
			rawConfig[0] |= (uint)1 << 16;
			// Depth
			rawConfig[0] |= (uint)1 << 24;
		}
    }
}
#nullable restore