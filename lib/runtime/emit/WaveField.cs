namespace wave.emit
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using static MoreLinq.Extensions.BatchExtension;
    using static WaveTypeCode;

    public record FieldName(string fullName)
    {
        public string Name => fullName.Split('.').Last();
        public string Class => fullName.Split('.').SkipLast(1).Join();
        
        
        public RuntimeToken Token 
            => RuntimeToken.Create(fullName);


        public static implicit operator string(FieldName t) => t.fullName;
        public static implicit operator FieldName(string t) => new(t);


        public FieldName(string name, string className) : this($"{className}.{name}") { }

        public static FieldName Construct(in int nameIdx, in int classIdx, WaveModule module) 
            => new(module.GetConstByIndex(nameIdx), module.GetConstByIndex(classIdx));
        public static FieldName Construct(in long fullIdx, WaveModule module) 
            => new(module.GetConstByIndex((int)(fullIdx >> 32)), 
                module.GetConstByIndex((int)(fullIdx & uint.MaxValue)));
    }
    [DebuggerDisplay("{ToString()}")]
    public class WaveField : WaveMember
    {
        public WaveField(WaveClass owner, FieldName fullName, FieldFlags flags, WaveType fieldType, object val = null)
        {
            this.Owner = owner;
            this.FullName = fullName;
            this.Flags = flags;
            this.FieldType = fieldType;
            this.litValue = val;
        }
        public FieldName FullName { get; protected internal set; }
        public WaveType FieldType { get; set; }
        public FieldFlags Flags { get; set; }
        public WaveClass Owner { get; set; }
        
        public override string Name 
        { 
            get; 
            protected set;
        }
        
        
        internal object LiteralFieldValue
        {
            get
            {
                if (!IsLiteral)
                    throw new InvalidOperationException("Cannot get literal value from non-literal field.");
                return litValue;
            }
            set
            {
                if (!IsLiteral)
                    throw new InvalidOperationException("Cannot set literal value into non-literal field.");
                litValue = value;
            }
        }
        public override WaveMemberKind Kind => WaveMemberKind.Field;
        
        
        public bool IsLiteral => this.Flags.HasFlag(FieldFlags.Literal);
        public bool IsStatic => this.Flags.HasFlag(FieldFlags.Static);
        public bool IsPublic => this.Flags.HasFlag(FieldFlags.Public);
        public bool IsPrivate => !IsPublic;


        private object litValue;
    }
    [Flags]
    public enum FieldFlags : byte
    {
        None = 0,
        Literal = 1 << 1,
        Public = 1 << 2,
        Static = 1 << 3,
        Protected = 1 << 4
    }
    
    public static class WaveFieldExtension
    {
        public static void WriteLiteralValue(this BinaryWriter binary, WaveField field)
        {
            if (!field.IsLiteral)
            {
                binary.Write(0);
                return;
            }
            var bytes = field.BakeLiteralValue();
            binary.Write(bytes.Length);
            binary.Write(bytes);
        }
        public static byte[] BakeLiteralValue(this WaveField field)
        {
            var val = field.LiteralFieldValue;
            
            if (new [] { TYPE_U1, TYPE_U2, TYPE_U4, TYPE_U8 }.Any(x => x == field.FieldType.TypeCode))
                throw new NotSupportedException("Unsigned integer is not support.");

            return (val, field.FieldType.TypeCode) switch
            {
                (bool b, TYPE_BOOLEAN)  => new byte[] {b ? 1 : 0},
                (char b, TYPE_CHAR)     => BitConverter.GetBytes(b),
                (byte b, TYPE_I1)       => new[] {b},
                (short b, TYPE_I2)      => BitConverter.GetBytes(b),
                (int b, TYPE_I4)        => BitConverter.GetBytes(b),
                (long b, TYPE_I8)       => BitConverter.GetBytes(b),
                (float b, TYPE_R4)      => BitConverter.GetBytes(b),
                (double b, TYPE_R8)     => BitConverter.GetBytes(b),
                (decimal b, TYPE_R16)   => decimal.GetBits(b).Select(BitConverter.GetBytes).SelectMany(x => x).ToArray(),
                (string b, TYPE_STRING) => Encoding.UTF8.GetBytes(b),
                _ => throw new InvalidOperationException($"Cannot bake literal value. [not primitive type.]")
            };
        }
        public static object ReadLiteralValue(this BinaryReader binary, WaveTypeCode code)
        {
            var size = binary.ReadInt32();
            
            if (new [] { TYPE_U1, TYPE_U2, TYPE_U4, TYPE_U8 }.Any(x => x == code))
                throw new NotSupportedException("Unsigned integer is not support.");

            return (code) switch
            {
                (TYPE_BOOLEAN)  => binary.ReadByte() == 1,
                (TYPE_CHAR)     => BitConverter.ToChar(binary.ReadBytes(size)),
                (TYPE_I1)       => binary.ReadByte(),
                (TYPE_I2)       => BitConverter.ToChar(binary.ReadBytes(size)),
                (TYPE_I4)       => BitConverter.ToChar(binary.ReadBytes(size)),
                (TYPE_I8)       => BitConverter.ToChar(binary.ReadBytes(size)),
                (TYPE_R4)       => BitConverter.ToChar(binary.ReadBytes(size)),
                (TYPE_R8)       => BitConverter.ToChar(binary.ReadBytes(size)),
                (TYPE_R16)      => new decimal(new ReadOnlySpan<int>(binary.ReadBytes(size)
                                                .Batch(sizeof(int))
                                                .Select(x => BitConverter.ToInt32(x.ToArray()))
                                                .ToArray())),
                (TYPE_STRING)   => Encoding.UTF8.GetString(binary.ReadBytes(size)),
                _ => null
            };
        }
    }
}