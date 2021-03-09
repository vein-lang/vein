namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class ConstStorage : IBaker
    {
        private readonly Dictionary<FieldName, object> storage = new();

        public void Stage(FieldName name, object o)
        {
            var type = o.GetType();

            if (!type.IsPrimitive)
                throw new ConstCannotUseNonPrimitiveTypeException(name, type);
            storage.Add(name, o);
        }

        public object Get(FieldName name) => storage[name];
        
        public byte[] BakeByteArray()
        {
            using var mem = new MemoryStream();
            using var bin = new BinaryWriter(mem);
            foreach (var (key, value) in storage)
            {
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Empty:
                    case TypeCode.Object:
                    case TypeCode.DateTime:
                    case TypeCode.Char:
                    case TypeCode.DBNull:
                        throw new ConstCannotUseNonPrimitiveTypeException(key, value.GetType());
                    case TypeCode.Boolean:
                        bin.Write((bool)value);
                        break;
                    case TypeCode.SByte:
                        bin.Write((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        bin.Write((byte)value);
                        break;
                    case TypeCode.Int16:
                        bin.Write((short)value);
                        break;
                    case TypeCode.UInt16:
                        bin.Write((ushort)value);
                        break;
                    case TypeCode.Int32:
                        bin.Write((int)value);
                        break;
                    case TypeCode.UInt32:
                        bin.Write((uint)value);
                        break;
                    case TypeCode.Int64:
                        bin.Write((long)value);
                        break;
                    case TypeCode.UInt64:
                        bin.Write((ulong)value);
                        break;
                    case TypeCode.Single:
                        bin.Write((float)value);
                        break;
                    case TypeCode.Double:
                        bin.Write((double)value);
                        break;
                    case TypeCode.Decimal:
                        bin.Write((decimal)value);
                        break;
                    case TypeCode.String:
                        bin.WriteInsomniaString((string)value);
                        break;
                }
            }

            return mem.ToArray();
        }
        

        public string BakeDebugString()
        {
            var str = new StringBuilder();
            foreach (var (key, value) in storage) 
                str.AppendLine($"@{key.fullName} = {value}");
            return str.ToString();
        }
    }
}