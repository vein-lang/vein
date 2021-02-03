namespace wave.emit
{
    using System.Collections.Generic;

    public class WaveClass
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public RuntimeToken Token { get; set; }
        public WaveClass Parent { get; set; }
        public readonly List<WaveClassField> Fields = new();
        public readonly List<WaveClassMethod> Methods = new();

        public WaveClass() { }

        public WaveClass(TypeName name, WaveClass parent)
        {
            this.Path = name.Namespace;
            this.Name = name.Name;
            this.Token = name.Token;
            this.Parent = parent;
        }
        
        public WaveClass DefineMethod(string name, WaveRuntimeType returnType, MethodFlags flags, params WaveArgumentRef[] args)
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