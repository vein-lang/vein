namespace mana.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using extensions;
    using static ManaTypeCode;

    public record FieldName(string fullName)
    {
        public string Name => fullName.Split('.').Last();
        public string Class => fullName.Split('.').SkipLast(1).Join();
        
        [Obsolete]
        public RuntimeToken Token 
            => RuntimeToken.Create(fullName);


        public static implicit operator string(FieldName t) => t.fullName;
        public static implicit operator FieldName(string t) => new(t);


        public FieldName(string name, string className) : this($"{className}.{name}") { }

        public static FieldName Construct(ManaClass owner, string name) 
            => new(name, owner.FullName.Name);

        public static FieldName Resolve(int index, ManaModule module)
        {
            var value = module.fields_table.GetValueOrDefault(index) ??
                        throw new Exception($"FieldName by index '{index}' not found in '{module.Name}' module.");
            return value;
        }
        
        public override string ToString() => $"{Class}.{Name}";
    }
    [DebuggerDisplay("{ToString()}")]
    public class ManaField : ManaMember
    {
        public ManaField(ManaClass owner, FieldName fullName, FieldFlags flags, ManaClass fieldType)
        {
            this.Owner = owner;
            this.FullName = fullName;
            this.Flags = flags;
            this.FieldType = fieldType;
        }
        public FieldName FullName { get; protected internal set; }
        public ManaClass FieldType { get; set; }
        public FieldFlags Flags { get; set; }
        public ManaClass Owner { get; set; }
        
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
        public static Func<string, object> GetConverter(this ManaTypeCode code)
        {
            if (new [] { TYPE_U1, TYPE_U2, TYPE_U4, TYPE_U8 }.Any(x => x == code))
                throw new NotSupportedException("Unsigned integer is not support.");

            return (code) switch
            {
                (TYPE_BOOLEAN)  => (x) => bool.Parse(x),
                (TYPE_CHAR)     => (x) => char.Parse(x),
                (TYPE_I1)       => (x) => byte.Parse(x),
                (TYPE_I2)       => (x) => short.Parse(x),
                (TYPE_I4)       => (x) => int.Parse(x),
                (TYPE_I8)       => (x) => long.Parse(x),
                (TYPE_R2)       => (x) => Half.Parse(x),
                (TYPE_R4)       => (x) => float.Parse(x),
                (TYPE_R8)       => (x) => double.Parse(x),
                (TYPE_R16)      => (x) => decimal.Parse(x),
                (TYPE_STRING)   => (x) => x,
                _ => throw new InvalidOperationException($"Cannot fetch converter for {code}.")
            };
        }

        public static Func<string, object> GetConverter(this ManaField field)
            => GetConverter(field.FieldType.TypeCode);
    }
}