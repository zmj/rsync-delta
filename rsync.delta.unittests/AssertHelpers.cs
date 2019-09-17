using System;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    internal static class AssertHelpers
    {
        public static void Equal(byte[] expected, byte[] actual)
        {
            if (expected.AsSpan().SequenceEqual(actual.AsSpan()))
            {
                return;
            }
            // string equal assert has nicely formatted output
            Assert.Equal(
                expected: BitConverter.ToString(expected),
                actual: BitConverter.ToString(actual));
        }
    }
}