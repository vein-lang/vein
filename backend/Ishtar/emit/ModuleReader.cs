namespace mana.ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;
    using insomnia;
    using reflection;
    using mana.extensions;
    using mana.runtime;

    internal static class BinaryExtension
    {
        public static string ReadInsomniaString(this BinaryReader reader)
        {
            reader.ValidateMagicFlag();
            var size = reader.ReadInt32();
            return Encoding.UTF8.GetString(reader.ReadBytes(size));
        }
        public static void WriteInsomniaString(this BinaryWriter writer, string value)
        {
            writer.WriteMagicFlag();
            var body = Encoding.UTF8.GetBytes(value);
            writer.Write(body.Length);
            writer.Write(body);
        }

        public static void ValidateMagicFlag(this BinaryReader reader)
        {
            var m1 = reader.ReadByte();
            var m2 = reader.ReadByte();
            if (m1 != 0xFF || m2 != 0xFF)
                throw new InvalidOperationException("Cannot read string from binary stream. [magic flag invalid]");
        }
        public static void WriteMagicFlag(this BinaryWriter writer)
        {
            writer.Write((byte)0xFF);
            writer.Write((byte)0xFF);
        }
    }

    internal class ModuleReader : ManaModule
    {
        public static ModuleReader Read(byte[] arr, List<ManaModule> deps, Func<string, Version, ManaModule> resolver)
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
                try
                {
                    var key = reader.ReadInt32();
                    var value = reader.ReadInsomniaString();
                    module.strings_table.Add(key, value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
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

                if (@class.IsSpecial)
                {
                    if (ManaCore.All.Any(x => x.FullName == @class.FullName))
                        TypeForwarder.Indicate(@class);
                }

                module.class_table.Add(@class);
            }

            // restore unresolved types
            foreach (var @class in module.class_table)
            {
                if (@class.Parent is not UnresolvedManaClass)
                    continue;
                @class.Parent = 
                    @class.Parent.FullName != @class.FullName ? 
                        module.FindType(@class.Parent.FullName, true) : 
                        null;
            }
            // restore unresolved types
            foreach (var @class in module.class_table)
            {
                foreach (var method in @class.Methods)
                {
                    if (method.ReturnType is not UnresolvedManaClass)
                        continue;
                    method.ReturnType = module.FindType(method.ReturnType.FullName, true);
                }

                foreach (var method in @class.Methods)
                {
                    foreach (var argument in method.Arguments)
                    {
                        if (argument.Type is not UnresolvedManaClass)
                            continue;
                        argument.Type = module.FindType(argument.Type.FullName, true);
                    }
                }
                foreach (var field in @class.Fields)
                {
                    if (field.FieldType is not UnresolvedManaClass)
                        continue;
                    field.FieldType = module.FindType(field.FieldType.FullName, true);
                }
            }

            var const_body_len = reader.ReadInt32();
            var const_body = reader.ReadBytes(const_body_len);

            module.Name = module.GetConstStringByIndex(idx);
            module.Version = Version.Parse(module.GetConstStringByIndex(vdx));

            return module;
        }


        public static ManaClass DecodeClass(byte[] arr, ModuleReader module)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var className = binary.ReadTypeName(module);
            var flags = (ClassFlags)binary.ReadInt16();
            var parentIdx = binary.ReadTypeName(module);
            var len = binary.ReadInt32();
            
            var @class = new ManaClass(className, module.FindType(parentIdx, true, false), module)
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
        
        public static void DecodeField(BinaryReader binary, ManaClass @class, ModuleReader module)
        {
            foreach (var _ in ..binary.ReadInt32())
            {
                var name = FieldName.Resolve(binary.ReadInt32(), module);
                var type_name = binary.ReadTypeName(module);
                var type = module.FindType(type_name, true, false);
                var flags = (FieldFlags) binary.ReadInt16();
                var method = new ManaField(@class, name, flags, type);
                @class.Fields.Add(method);
            }
        }
        
        public static ManaMethod DecodeMethod(byte[] arr, ManaClass @class, ModuleReader module)
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
            return new ManaMethod(module.GetConstStringByIndex(idx), flags,
                module.FindType(retType, true, false), 
                @class, args.ToArray());
        }

        
        
        private static List<ManaArgumentRef> ReadArguments(BinaryReader binary, ModuleReader module)
        {
            var args = new List<ManaArgumentRef>();
            foreach (var _ in ..binary.ReadInt32())
            {
                var nIdx = binary.ReadInt32();
                var type = binary.ReadTypeName(module);
                args.Add(new ManaArgumentRef
                {
                    Name = module.GetConstStringByIndex(nIdx),
                    Type = module.FindType(type, true, false)
                });
            }
            return args;
        }
        public ModuleReader() : base(null)
        {
        }
    }
}