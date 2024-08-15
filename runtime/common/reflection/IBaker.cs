namespace vein.runtime
{
    public interface IBaker
    {
        byte[] BakeByteArray();
        string BakeDebugString();
        string BakeDiagnosticDebugString();
    }
}
