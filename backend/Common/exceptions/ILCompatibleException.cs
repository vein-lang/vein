namespace mana.exceptions
{
    using System;

    public class ILCompatibleException : Exception
    {
        public ILCompatibleException(int low, int hi)
            : base($"Detected '0x{low:X}' IL version is not compatible with '0x{hi:X}' IL codebase version.")
        {

        }
    }
}