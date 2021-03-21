namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using extensions;
    using wave;
    
    internal static class BinaryExtension
    {
        public static string ReadInsomniaString(this BinaryReader reader)
        {
            var size = reader.ReadInt32();
            var magic = reader.ReadByte();
            if (magic != 0x45)
                throw new InvalidOperationException("Cannot read string from binary stream. [magic flag invalid]");
            return Encoding.UTF8.GetString(reader.ReadBytes(size));
        }
        public static void WriteInsomniaString(this BinaryWriter writer, string value)
        {
            var body = Encoding.UTF8.GetBytes(value);
            writer.Write(body.Length);
            writer.Write((byte)0x45);
            writer.Write(body);
        }
    }

    internal class ModuleReader : WaveModule
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
                module.strings.Add(key, reader.ReadInsomniaString());
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
            var className = binary.ReadTypeName(module);
            var flags = (ClassFlags)binary.ReadByte();
            var parentIdx = binary.ReadTypeName(module);
            var len = binary.ReadInt32();

            var parent = module.FindType(parentIdx, true);
            var @class = new WaveClass(className, parent.AsClass())
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
                var type_name = binary.ReadTypeName(module);
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
            var idx = binary.ReadInt32();
            var flags = (MethodFlags)binary.ReadByte();
            var bodysize = binary.ReadInt32();
            var stacksize = binary.ReadByte();
            var locals = binary.ReadByte();
            var retType = binary.ReadTypeName(module);
            var args = ReadArguments(binary, module);
            var _ = binary.ReadBytes(bodysize);
            return new WaveMethod(module.GetConstByIndex(idx), flags,
                module.FindType(retType, true), 
                @class, args.ToArray());
        }

        
        
        private static List<WaveArgumentRef> ReadArguments(BinaryReader binary, ModuleReader module)
        {
            var args = new List<WaveArgumentRef>();
            foreach (var _ in ..binary.ReadInt32())
            {
                var nIdx = binary.ReadInt32();
                var type = binary.ReadTypeName(module);
                args.Add(new WaveArgumentRef()
                {
                    Name = module.GetConstByIndex(nIdx),
                    Type = module.FindType(type, true)
                });
            }
            return args;
        }
        public ModuleReader() : base(null)
        {
        }
    }
}