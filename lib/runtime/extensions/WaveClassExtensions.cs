namespace wave.emit
{
    using System.Linq;

    public static class WaveClassExtensions
    {
        public static WaveType AsType(this WaveClass @class)
        {
            var result = new WaveTypeImpl(@class.FullName, @class.TypeCode, @class.Flags, @class.Parent?.AsType());
            
            result.Members.AddRange(@class.Methods);
            result.Members.AddRange(@class.Fields);
            return result;
        }
        public static WaveClass AsClass(this WaveType type)
        {
            var result = new WaveClass(type.FullName, type.Parent?.AsClass())
            {
                Flags = type.classFlags ?? ClassFlags.None, 
                TypeCode = type.TypeCode
            };
            result.Methods.AddRange(type.Members.OfType<WaveMethod>());
            result.Fields.AddRange(type.Members.OfType<WaveField>());
            return result;
        }
    }
}