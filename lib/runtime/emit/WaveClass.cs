namespace wave.emit
{
    using System.Collections.Generic;
    using System.Linq;

    public class WaveClass
    {
        public QualityTypeName FullName { get; set; }
        public string Name => FullName.Name;
        public string Path => FullName.Namespace;
        public ClassFlags Flags { get; set; }
        public WaveClass Parent { get; set; }
        public readonly List<WaveField> Fields = new();
        public readonly List<WaveMethod> Methods = new();
        public WaveTypeCode TypeCode { get; set; } = WaveTypeCode.TYPE_CLASS;
        
        internal WaveClass(QualityTypeName name, WaveClass parent)
        {
            this.FullName = name;
            this.Parent = parent;
        }
        internal WaveClass(WaveType type, WaveClass parent)
        {
            this.FullName = type.FullName;
            this.Parent = parent;
        }
        
        protected WaveClass() {  }
        
        internal WaveMethod DefineMethod(string name, WaveType returnType, MethodFlags flags, params WaveArgumentRef[] args)
        {
            var method = new WaveMethod(name, flags, returnType, this, args);
            method.Arguments.AddRange(args);
            Methods.Add(method);
            return method;
        }

        internal WaveMethod FindMethod(string name) 
            => Methods.FirstOrDefault(method => method.Name == name);
    }
}