namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using exceptions;
    using extensions;
    using reflection;
    using static VeinTypeCode;

    public record FieldName(string fullName)
    {
        public string Name => fullName.Split('.').Last();
        public string Class => fullName.Split('.').SkipLast(1).Join();


        public static implicit operator string(FieldName t) => t.fullName;
        public static implicit operator FieldName(string t) => new(t);


        public FieldName(string name, string className) : this($"{className}.{name}") { }

        public static FieldName Construct(VeinClass owner, string name)
            => new(name, owner.FullName.Name);

        public static FieldName Resolve(int index, ManaModule module)
        {
            var value = module.fields_table.GetValueOrDefault(index) ??
                        throw new Exception($"FieldName by index '{index}' not found in '{module.Name}' module.");
            return value;
        }

        public override string ToString() => $"{Class}.{Name}";
    }
    public class VeinField : VeinMember, IAspectable
    {
        public VeinField(VeinClass owner, FieldName fullName, FieldFlags flags, VeinClass fieldType)
        {
            this.Owner = owner;
            this.FullName = fullName;
            this.Flags = flags;
            this.FieldType = fieldType;
        }
        public FieldName FullName { get; protected internal set; }
        public VeinClass FieldType { get; set; }
        public FieldFlags Flags { get; set; }
        public VeinClass Owner { get; set; }
        public List<Aspect> Aspects { get; } = new();

        public override string Name
        {
            get => FullName.Name;
            protected set => throw new NotImplementedException();
        }
        public override ManaMemberKind Kind => ManaMemberKind.Field;


        public bool IsLiteral => this.Flags.HasFlag(FieldFlags.Literal);
        public bool IsStatic => this.Flags.HasFlag(FieldFlags.Static);
        public bool IsPublic => this.Flags.HasFlag(FieldFlags.Public);
        public bool IsPrivate => !IsPublic;
    }

    public static class ManaFieldExtension
    {
        public static Func<string, object> GetConverter(this VeinTypeCode code)
        {
            Func<string, object> result = (code) switch
            {
                (TYPE_BOOLEAN)  => (x) => bool.Parse(x),
                (TYPE_CHAR)     => (x) => char.Parse(x),
                (TYPE_I1)       => (x) => sbyte.Parse(x),
                (TYPE_I2)       => (x) => short.Parse(x),
                (TYPE_I4)       => (x) => int.Parse(x),
                (TYPE_I8)       => (x) => long.Parse(x),
                (TYPE_U1)       => (x) => byte.Parse(x),
                (TYPE_U2)       => (x) => ushort.Parse(x),
                (TYPE_U4)       => (x) => uint.Parse(x),
                (TYPE_U8)       => (x) => ulong.Parse(x),
                (TYPE_R2)       => (x) => Half.Parse(x),
                (TYPE_R4)       => (x) => float.Parse(x),
                (TYPE_R8)       => (x) => double.Parse(x),
                (TYPE_R16)      => (x) => decimal.Parse(x),
                (TYPE_STRING)   => (x) => x,
                _ => throw new NotSupportedException($"Cannot fetch converter for {code}.")
            };

            return WrapConverter(result, code);
        }

        private static Func<string, object> WrapConverter(Func<string, object> actor, VeinTypeCode typeCode) => x =>
        {
            try
            {
                return actor(x);
            }
            catch (OverflowException e)
            {
                throw new ValueWasIncorrectException(x, typeCode, e);
            }
        };

        public static Func<string, object> GetConverter(this VeinField field)
        {
            try
            {
                return GetConverter(field.FieldType.TypeCode);
            }
            catch (NotSupportedException)
            {
                throw new ConvertNotSupportedException(field);
            }
        }
    }


}
