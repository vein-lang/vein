namespace mana.ishtar.emit.extensions
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using exceptions;
    using mana.extensions;
    using mana.runtime;

    public static class ConstStorageExtensions
    {
        public static byte[] BakeByteArray(this ConstStorage cs)
        {
            using var mem = new MemoryStream();
            using var bin = new BinaryWriter(mem);
            bin.Write(cs.storage.Count);
            foreach (var (key, value) in cs.storage)
            {
                try
                {
                    var type_code = value.DetermineTypeCode();

                    bin.Write((int)type_code);
                    bin.WriteInsomniaString(key.fullName);
                    bin.WriteInsomniaString(value.ToString());
                }
                catch (NotSupportedException e)
                {
                    throw new ObjectIsNotValueType($"{e.Message}, '{value}', {value.GetType()}, '{key}'");
                }
               
            }

            return mem.ToArray(); // (int)Convert.ChangeType("12", typeof(int))
        }

        public static ConstStorage ToConstStorage(this byte[] arr)
        {
            using var mem = new MemoryStream(arr);
            using var bin = new BinaryReader(mem);
            var storage = new ConstStorage();
            foreach (var i in ..bin.ReadInt32())
            {
                var type_code = (ManaTypeCode)bin.ReadInt32();
                var fullname = bin.ReadInsomniaString();
                var value = bin.ReadInsomniaString();
                storage.Stage(new FieldName(fullname), Convert.ChangeType(value, type_code.ToCLRTypeCode()));
            }

            return storage;
        }
        public static string BakeDebugString(this ConstStorage cs)
        {
            var str = new StringBuilder();
            foreach (var (key, value) in cs.storage)
                str.AppendLine($"@'{key.fullName}' = '{value}'");
            return str.ToString();
        }
    }
}