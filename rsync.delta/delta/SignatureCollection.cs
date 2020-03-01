﻿using System;
using System.Collections.Generic;
using System.Text;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;

namespace Rsync.Delta.Delta
{
    internal sealed class SignatureCollection
        <TRollingHashAlgorithm, TStrongHashAlgoritm> 
        : Dictionary<BlockSignature<TRollingHashAlgorithm, TStrongHashAlgoritm>, ulong>
        where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
        where TStrongHashAlgoritm : IStrongHashAlgorithm
    {
    }
}