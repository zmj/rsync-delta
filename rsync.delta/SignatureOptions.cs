namespace Rsync.Delta
{
    public readonly struct SignatureOptions
    {
        public readonly uint BlockLength;
        public readonly uint StrongHashLength;

        public SignatureOptions(uint blockLength, uint strongHashLength)
        {
            BlockLength = blockLength;
            StrongHashLength = strongHashLength;
        }

        public static SignatureOptions Default =>
            new SignatureOptions(blockLength: 2048, strongHashLength: 32);
    }
}