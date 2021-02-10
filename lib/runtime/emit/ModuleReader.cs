namespace wave.emit
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using wave;

    public class ModuleReader : WaveModule
    {
        public static ModuleReader Read(byte[] arr, List<WaveModule> deps)
        {
            var module = new ModuleReader();
            using var mem = new MemoryStream(arr);
            using var reader = new BinaryReader(mem);
            module.Deps.AddRange(deps);

            var idx = reader.ReadInt32();
            
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                module.strings.Add(key, reader.ReadString());
            }

            foreach (var _ in ..reader.ReadInt32())
            {
                var body = reader.ReadBytes(reader.ReadInt32());
                var @class = DecodeClass(body, module);
                module.classList.Add(@class);
            }

            module.Name = module.GetConstByIndex(idx);

            return module;
        }


        public static WaveClass DecodeClass(byte[] arr, ModuleReader module)
        {
           
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var idx = binary.ReadInt32();
            var nsidx = binary.ReadInt32();
            var flags = (ClassFlags)binary.ReadUInt32();
            var parentIdx = binary.ReadInt64();
            var len = binary.ReadInt32();

            var parent = module.FindType(TypeName.Construct(parentIdx, module), true);
            var @class = new WaveClass(TypeName.Construct(idx, nsidx, module), parent.AsClass())
            {
                Flags = flags
            };
            foreach (var _ in ..len)
            {
                var body = 
                    binary.ReadBytes(binary.ReadInt32());
                var method = DecodeMethod(body, @class, module);
                @class.Methods.Add(method);
            }

            DecodeField(binary, @class, module);

            return @class;
        }
        
        public static void DecodeField(BinaryReader binary, WaveClass @class, ModuleReader module)
        {
            foreach (var _ in ..binary.ReadInt32())
            {
                var name = FieldName.Construct(binary.ReadInt64(), module);
                var type_name = TypeName.Construct(binary.ReadInt64(), module);
                var type = module.FindType(type_name, true);
                var flags = (FieldFlags) binary.ReadByte();
                var litValue = binary.ReadLiteralValue(type.TypeCode);
                var method = new WaveField(@class, name, flags, type, litValue);
                @class.Fields.Add(method);
            }
        }
        
        public static WaveMethod DecodeMethod(byte[] arr, WaveClass @class, ModuleReader module)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            /*
             *  binary.Write(idx); // $method name
            binary.Write((byte)Flags); // $flags
            binary.Write(body.Length); // body size
            binary.Write((byte)64); // stack size TODO
            binary.Write((byte)24); // locals size TODO
            binary.Write(classBuilder.moduleBuilder.GetTypeConstant(ReturnType.FullName));
            WriteArguments(binary);
            binary.Write(body); // IL Body

             */
            var idx = binary.ReadInt32();
            var flags = (MethodFlags)binary.ReadByte();
            var bodysize = binary.ReadInt32();
            var stacksize = binary.ReadByte();
            var locals = binary.ReadByte();
            var retTypeIdx = binary.ReadInt64();
            var args = ReadArguments(binary, module);
            var _ = binary.ReadBytes(bodysize);
            return new WaveMethod(module.GetConstByIndex(idx), flags,
                module.FindType(TypeName.Construct(retTypeIdx, module), true), 
                @class, args.ToArray());
        }

        
        
        private static List<WaveArgumentRef> ReadArguments(BinaryReader binary, ModuleReader module)
        {
            var args = new List<WaveArgumentRef>();
            foreach (var _ in ..binary.ReadInt32())
            {
                var nIdx = binary.ReadInt32();
                var tIdx = binary.ReadInt64();
                args.Add(new WaveArgumentRef()
                {
                    Name = module.GetConstByIndex(nIdx),
                    Type = module.FindType(TypeName.Construct(tIdx, module), true)
                });
            }
            return args;
        }
        public ModuleReader() : base(null)
        {
        }
    }
}