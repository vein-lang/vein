namespace mana.runtime
{
    using System.Linq;

    public static class ManaClassExtensions
    {
        public static ManaType AsType(this ManaClass @class)
        {
            var result = new ManaTypeImpl(@class.FullName, @class.TypeCode, @class.Flags, @class.Parent?.AsType());

            result.Members.AddRange(@class.Methods);
            result.Members.AddRange(@class.Fields);
            return result;
        }
        public static ManaClass AsClass(this ManaType type)
        {
            var result = new ManaClass(type.FullName, type.Parent?.AsClass(), type.Owner)
            {
                Flags = type.classFlags ?? ClassFlags.None,
                TypeCode = type.TypeCode
            };
            result.Methods.AddRange(type.Members.OfType<ManaMethod>());
            result.Fields.AddRange(type.Members.OfType<ManaField>());
            return result;
        }
    }
}