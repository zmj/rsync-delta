using System;
using System.Collections.Generic;
using System.Text;

namespace Rsync.Delta.Hash
{
    internal ref partial struct Blake2bCore
    {
        private static class Constants
        {
            public const int Rounds = 12;

            public static readonly int[] MessagePermutation = new int[16]
            {
                // round 1
                0,  2,  4,  6,
                1,  3,  5,  7,
                8,  10, 12, 14,
                9,  11, 13, 15,
            };
        }
    }
}
