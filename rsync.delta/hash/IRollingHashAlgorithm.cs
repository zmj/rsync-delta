namespace Rsync.Delta.Hash
{
    internal interface IRollingHashAlgorithm
    {
        int Reset();
        int RotateIn(byte add);
        int Rotate(byte remove, byte add);
        int RotateOut(byte remove);
    }
}
