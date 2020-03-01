using System;
using System.Collections.Generic;
using System.Text;
using Rsync.Delta.Models;

namespace Rsync.Delta.Delta
{
    internal sealed class SignatureCollection : Dictionary<BlockSignature, ulong>
    {
    }
}
