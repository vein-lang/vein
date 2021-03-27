namespace insomnia.emit
{
    public interface IBaker
    {
        byte[] BakeByteArray();
        string BakeDebugString();
    }
}