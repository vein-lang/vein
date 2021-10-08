namespace ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using extensions;
    using global::ishtar;
    using vein.exceptions;
    using vein.reflection;
    using vein.extensions;
    using vein.runtime;

    internal static class BinaryExtension
    {
        [MethodImpl(MethodImplOptions.NoOptimization)] // what the hell clr
        public static string ReadVeinString(this BinaryReader reader)
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
            var ilVersion = reader.ReadInt32();

            if (ilVersion != OpCodes.SetVersion)
                throw new ILCompatibleException(ilVersion, OpCodes.SetVersion);
            // read strings table
            foreach (var _ in ..reader.ReadInt32())
            {
                try
                {
                    var key = reader.ReadInt32();
                    var value = reader.ReadVeinString();
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
                var asmName = reader.ReadVeinString();
                var ns = reader.ReadVeinString();
                var name = reader.ReadVeinString();
                module.types_table.Add(key, new QualityTypeName(asmName, name, ns));
            }
            // read fields table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var name = reader.ReadVeinString();
                var clazz = reader.ReadVeinString();
                module.fields_table.Add(key, new FieldName(name, clazz));
            }

            // read deps refs
            foreach (var _ in ..reader.ReadInt32())
            {
                var name = reader.ReadVeinString();
                var ver = Version.Parse(reader.ReadVeinString());
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
                    if (VeinCore.All.Any(x => x.FullName == @class.FullName))
                        TypeForwarder.Indicate(@class);
                }

                module.class_table.Add(@class);
            }

            // restore unresolved types
            foreach (var @class in module.class_table)
            {
                for (var index = 0; index < @class.Parents.Count; index++)
                {
                    var parent = @class.Parents[index];
                    if (parent is not UnresolvedVeinClass)
                        continue;
                    @class.Parents[index] =
                        parent.FullName != @class.FullName
                            ? module.FindType(parent.FullName, true)
                            : null;
                }
            }
            // restore unresolved types
            foreach (var @class in module.class_table)
            {
                foreach (var method in @class.Methods)
                {
                    if (method.ReturnType is not UnresolvedVeinClass)
                        continue;
                    method.ReturnType = module.FindType(method.ReturnType.FullName, true);
                }

                foreach (var method in @class.Methods)
                {
                    foreach (var argument in method.Arguments)
                    {
                        if (argument.Type is not UnresolvedVeinClass)
                            continue;
                        argument.Type = module.FindType(argument.Type.FullName, true);
                    }
                }
                foreach (var field in @class.Fields)
                {
                    if (field.FieldType is not UnresolvedVeinClass)
                        continue;
                    field.FieldType = module.FindType(field.FieldType.FullName, true);
                }
            }

            var const_body_len = reader.ReadInt32();
            var const_body = reader.ReadBytes(const_body_len);

            module.const_table = const_body.ToConstStorage();

            module.Name = module.GetConstStringByIndex(idx);
            module.Version = Version.Parse(module.GetConstStringByIndex(vdx));
            DistributionAspects(module);

            return module;
        }

        // shit, todo: refactoring
        public static void DistributionAspects(ModuleReader module)
        {
            foreach (var aspect in module.aspects)
            {
                switch (aspect)
                {
                    case AspectOfClass classAspect:
                        {
                            foreach (var @class in module.class_table
                                .Where(@class => @class.Name.Equals(classAspect.ClassName)))
                                @class.Aspects.Add(aspect);
                            break;
                        }
                    case AspectOfMethod methodAspect:
                        {
                            foreach (var method in module.class_table
                                .Where(@class => @class.Name.Equals(methodAspect.ClassName))
                                .SelectMany(@class => @class.Methods
                                    .Where(method => method.RawName.Equals(methodAspect.MethodName))))
                                method.Aspects.Add(aspect);
                            break;
                        }
                    case AspectOfField fieldAspect:
                        {
                            foreach (var field in module.class_table
                                .Where(@class => @class.Name.Equals(fieldAspect.ClassName))
                                .SelectMany(@class => @class.Fields
                                    .Where(field => field.Name.Equals(fieldAspect.FieldName))))
                                field.Aspects.Add(aspect);
                            break;
                        }
                }
            }
        }

        public static VeinClass DecodeClass(byte[] arr, ModuleReader module)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var className = binary.ReadTypeName(module);
            var flags = (ClassFlags)binary.ReadInt16();

            var parentLen = binary.ReadInt16();

            var parents = new List<VeinClass>();
            foreach (var _ in ..parentLen)
            {
                var parentIdx = binary.ReadTypeName(module);
                parents.Add(module.FindType(parentIdx, true, false));
            }
            
            var len = binary.ReadInt32();

            var @class = new VeinClass(className, parents.ToArray(), module)
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

        public static void DecodeField(BinaryReader binary, VeinClass @class, ModuleReader module)
        {
            foreach (var _ in ..binary.ReadInt32())
            {
                var name = FieldName.Resolve(binary.ReadInt32(), module);
                var type_name = binary.ReadTypeName(module);
                var type = module.FindType(type_name, true, false);
                var flags = (FieldFlags) binary.ReadInt16();
                var method = new VeinField(@class, name, flags, type);
                @class.Fields.Add(method);
            }
        }

        public static VeinMethod DecodeMethod(byte[] arr, VeinClass @class, ModuleReader module)
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
            return new VeinMethod(module.GetConstStringByIndex(idx), flags,
                module.FindType(retType, true, false),
                @class, args.ToArray());
        }



        private static List<VeinArgumentRef> ReadArguments(BinaryReader binary, ModuleReader module)
        {
            var args = new List<VeinArgumentRef>();
            foreach (var _ in ..binary.ReadInt32())
            {
                var nIdx = binary.ReadInt32();
                var type = binary.ReadTypeName(module);
                args.Add(new VeinArgumentRef
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
