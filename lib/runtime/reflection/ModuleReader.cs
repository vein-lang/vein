namespace insomnia.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;
    using insomnia;
    
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
        public static ModuleReader Read(byte[] arr, List<WaveModule> deps, Func<string, Version, WaveModule> resolver)
        {
            var module = new ModuleReader();
            using var mem = new MemoryStream(arr);
            using var reader = new BinaryReader(mem);
            module.Deps.AddRange(deps);

            var idx = reader.ReadInt32(); // name index
            var vdx = reader.ReadInt32(); // version index
           
            // read strings table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var value = reader.ReadInsomniaString();
                module.strings_table.Add(key, value);
            }
            // read types table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var asmName = reader.ReadInsomniaString();
                var ns = reader.ReadInsomniaString();
                var name = reader.ReadInsomniaString();
                module.types_table.Add(key, new QualityTypeName(asmName, name, ns));
            }
            // read fields table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var name = reader.ReadInsomniaString();
                var clazz = reader.ReadInsomniaString();
                module.fields_table.Add(key, new FieldName(name, clazz));
            }
            
            // read deps refs
            foreach (var _ in ..reader.ReadInt32())
            {
                var name = reader.ReadInsomniaString();
                var ver = Version.Parse(reader.ReadInsomniaString());
                if (module.Deps.Any(x => x.Version.Equals(ver) && x.Name.Equals(name))) 
                    continue;
                var dep = resolver(name, ver);
                module.Deps.Add(dep);
            }
            // read class storage
            foreach (var _ in ..reader.ReadInt32())
            {
                var body = reader.ReadBytes(reader.ReadInt32());
                var @class = DecodeClass(body, module);
                module.classList.Add(@class);
            }

            module.Name = module.GetConstStringByIndex(idx);
            module.Version = Version.Parse(module.GetConstStringByIndex(vdx));

            return module;
        }


        public static WaveClass DecodeClass(byte[] arr, ModuleReader module)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var className = binary.ReadTypeName(module);
            var flags = (ClassFlags)binary.ReadInt16();
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
                var name = FieldName.Resolve(binary.ReadInt32(), module);
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
            var flags = (MethodFlags)binary.ReadInt16();
            var bodysize = binary.ReadInt32();
            var stacksize = binary.ReadByte();
            var locals = binary.ReadByte();
            var retType = binary.ReadTypeName(module);
            var args = ReadArguments(binary, module);
            var _ = binary.ReadBytes(bodysize);
            return new WaveMethod(module.GetConstStringByIndex(idx), flags,
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
                    Name = module.GetConstStringByIndex(nIdx),
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