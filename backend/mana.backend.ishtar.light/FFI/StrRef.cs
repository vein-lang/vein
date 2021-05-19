namespace ishtar
{
    public unsafe struct StrRef
    {
        public static string Unwrap(StrRef* p) => StringStorage.GetString(p);

        public ulong index;
    }
}