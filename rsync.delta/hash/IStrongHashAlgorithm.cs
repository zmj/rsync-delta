﻿using System;
using System.Buffers;

namespace Rsync.Delta.Hash
{
    internal interface IStrongHashAlgorithm : IDisposable
    {
        void Hash(in ReadOnlySequence<byte> data, Span<byte> hash);
    }
}
