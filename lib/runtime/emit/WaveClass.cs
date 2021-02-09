namespace wave.emit
{
    using System.Collections.Generic;

    public class WaveClass
    {
        public WaveType Type { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public RuntimeToken Token { get; set; }
        public WaveClass Parent { get; set; }
        public readonly List<WaveClassField> Fields = new();
        public readonly List<WaveClassMethod> Methods = new();

        public WaveClass() { }

        public WaveClass(WaveType name, WaveClass parent)
        {
            this.Path = name.Namespace;
            this.Name = name.Name;
            this.Token = name.FullName.Token;
            this.Parent = parent;
        }
        
        public WaveClass DefineMethod(string name, WaveType returnType, MethodFlags flags, params WaveArgumentRef[] args)
        {
            var method = new WaveClassMethod
            {
                Flags = flags,
                Name = name,
                ReturnType = returnType,
                Owner = this
            };
            method.Arguments.AddRange(args);
            Methods.Add(method);
            return this;
        }
    }
}