namespace ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using extensions;
    using ishtar;
    using vein.exceptions;
    using vein.reflection;
    using vein.extensions;
    using vein.runtime;

    internal class MagicNumberArmor : IDisposable
    {
        private readonly BinaryWriter _bin;

        public MagicNumberArmor(BinaryWriter bin)
        {
            _bin = bin;
            _bin.Write(0x19);
        }

        public void Dispose() => _bin.Write(0x61);
    }

    internal static class BinaryExtension
    {
        [MethodImpl(MethodImplOptions.NoOptimization)] // what the hell clr
        public static string ReadIshtarString(this BinaryReader reader)
        {
            reader.ValidateMagicFlag();
            var size = reader.ReadInt32();
            return Encoding.UTF8.GetString(reader.ReadBytes(size));
        }
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void WriteIshtarString(this BinaryWriter writer, string value)
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

        public static int[] ReadIntArray(this BinaryReader bin)
        {
            Debug.Assert(bin.ReadInt32() == 0x19, "[magic number] bin.ReadInt32() == 0x19");
            var sign = bin.ReadIshtarString();
            var size = bin.ReadInt32();
            var result = new List<int>();
            foreach (int i in ..size)
                result.Add(bin.ReadInt32());
            Debug.Assert(bin.ReadInt32() == 0x61, "[magic number] bin.ReadInt32() == 0x61");
            return result.ToArray();
        }

        public static QualityTypeName[] ReadTypesArray(this BinaryReader bin, VeinModule module)
        {
            Debug.Assert(bin.ReadInt32() == 0x19, "[magic number] bin.ReadInt32() == 0x19");
            var sign = bin.ReadIshtarString();
            var size = bin.ReadInt32();
            var result = new List<QualityTypeName>();
            foreach (int i in ..size)
                result.Add(bin.ReadTypeName(module));
            Debug.Assert(bin.ReadInt32() == 0x61, "[magic number] bin.ReadInt32() == 0x61");
            return result.ToArray();
        }

        public static List<nint> ReadTypesArray(this BinaryReader bin, TypeNameGetter getter)
        {
            Debug.Assert(bin.ReadInt32() == 0x19, "[magic number] bin.ReadInt32() == 0x19");
            var sign = bin.ReadIshtarString();
            var size = bin.ReadInt32();
            var result = new List<nint>();
            foreach (int i in ..size)
                result.Add(bin.ReadTypeName(getter));
            Debug.Assert(bin.ReadInt32() == 0x61, "[magic number] bin.ReadInt32() == 0x61");
            return result;
        }


        public static T[] ReadSpecialByteArray<T>(this BinaryReader bin) where T : Enum
        {
            Debug.Assert(bin.ReadInt32() == 0x19, "[magic number] bin.ReadInt32() == 0x19");
            var sign = bin.ReadIshtarString();
            var size = bin.ReadInt32();
            var result = new List<T>();
            foreach (int _ in ..size)
                result.Add((T)(object)bin.ReadByte());
            Debug.Assert(bin.ReadInt32() == 0x61, "[magic number] bin.ReadInt32() == 0x61");
            return result.ToArray();
        }

        public static T[] ReadSpecialInt32Array<T>(this BinaryReader bin) where T : Enum
        {
            Debug.Assert(bin.ReadInt32() == 0x19, "[magic number] bin.ReadInt32() == 0x19");
            var sign = bin.ReadIshtarString();
            var size = bin.ReadInt32();
            var result = new List<T>();
            foreach (int _ in ..size)
                result.Add((T)(object)bin.ReadInt32());
            Debug.Assert(bin.ReadInt32() == 0x61, "[magic number] bin.ReadInt32() == 0x61");
            return result.ToArray();
        }

        public static byte[] ReadSpecialDirectByteArray(this BinaryReader bin)
        {
            Debug.Assert(bin.ReadInt32() == 0x19, "[magic number] bin.ReadInt32() == 0x19");
            var sign = bin.ReadIshtarString();
            var size = bin.ReadInt32();
            var result = new List<byte>();
            foreach (int _ in ..size)
                result.Add(bin.ReadByte());
            Debug.Assert(bin.ReadInt32() == 0x61, "[magic number] bin.ReadInt32() == 0x61");
            return result.ToArray();
        }


        public static void WriteArray<T>(this BinaryWriter bin, T[] arr, Action<T, BinaryWriter> selector, [CallerArgumentExpression("arr")] string name = "")
        {
            using (new MagicNumberArmor(bin))
            {
                bin.WriteIshtarString(name);
                bin.Write(arr.Length);
                foreach (var i in arr)
                    selector(i, bin);
            }
        }
    }

    internal class ModuleReader(VeinCore types) : VeinModule(new ModuleNameSymbol(".unnamed"), types)
    {
        public static ModuleReader Read(byte[] arr, IReadOnlyList<VeinModule> deps, Func<string, Version, VeinModule> resolver)
        {
            var types = new VeinCore();
            var module = new ModuleReader(types);
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
                    var value = reader.ReadIshtarString();
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
                var asmName = reader.ReadIshtarString();
                var ns = reader.ReadIshtarString();
                var name = reader.ReadIshtarString();
                module.types_table.Add(key, new QualityTypeName(new NameSymbol(name), new NamespaceSymbol(ns), new ModuleNameSymbol(asmName)));
            }
            // read fields table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var name = reader.ReadIshtarString();
                var clazz = reader.ReadIshtarString();
                module.fields_table.Add(key, new FieldName(name, clazz));
            }

            // read deps refs
            foreach (var _ in ..reader.ReadInt32())
            {
                var name = reader.ReadIshtarString();
                var ver = Version.Parse(reader.ReadIshtarString());
                if (module.Deps.Any(x => x.Version.Equals(ver) && x.Name.Equals(name)))
                    continue;
                var dep = resolver(name, ver);
                module.Deps.Add(dep);
            }

            var deferedMethods = new List<(VeinClass, byte[])>();
            // read class storage
            foreach (var _ in ..reader.ReadInt32())
            {
                var body = reader.ReadBytes(reader.ReadInt32());
                var @class = DecodeClass(body, module, deferedMethods);

                if (@class.IsSpecial)
                {
                    if (module.Types.All.Any(x => x.FullName == @class.FullName))
                        TypeForwarder.Indicate(types, @class);
                }

                module.class_table.Add(@class);
            }
            // read aliases
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var name = reader.ReadTypeName(module);
                var isType = reader.ReadBoolean();

                if (isType)
                {
                    var typename = reader.ReadTypeName(module);
                    module.alias_table.Add(new VeinAliasType(name, module.FindType(typename, true)));
                }
                else
                {
                    var retType = reader.ReadComplexType(module);
                    var args = ReadArguments(reader, module);
                    module.alias_table.Add(new VeinAliasMethod(name, new VeinMethodSignature(retType, args, new List<VeinTypeArg>() /* todo */)));
                }
            }

            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var name = reader.ReadIshtarString();
                var cts = new List<VeinBaseConstraint>();

                foreach (var count in ..reader.ReadInt32())
                {
                    var k = reader.ReadInt32();
                    var kind = (VeinTypeParameterConstraint)reader.ReadInt32();

                    if (kind == VeinTypeParameterConstraint.BITTABLE)
                        cts.Add(new VeinBaseConstraintConstBittable());
                    else if (kind == VeinTypeParameterConstraint.CLASS)
                        cts.Add(new VeinBaseConstraintConstClass());
                    else if (kind == VeinTypeParameterConstraint.TYPE)
                    {
                        var type = reader.ReadTypeName(module);
                        cts.Add(new VeinBaseConstraintConstType(module.FindType(type, true)));
                    }
                    else if (kind == VeinTypeParameterConstraint.SIGNATURE)
                    {
                        var type = reader.ReadTypeName(module);
                        cts.Add(new VeinBaseConstraintConstSignature(module.FindType(type, true)));
                    }
                    else
                        throw new NotImplementedException();
                }

                module.generics_table.Add(key, new VeinTypeArg(name, cts));
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
            // defer init methods
            foreach (var (@class, body) in deferedMethods)
            {
                var method = DecodeMethod(body, @class, module);
                @class.Methods.Add(method);
            }
            // restore unresolved types
            foreach (var @class in module.class_table)
            {
                foreach (var method in @class.Methods)
                {
                    if (method.ReturnType.IsGeneric)
                        continue;
                    if (method.ReturnType.Class is not UnresolvedVeinClass)
                        continue;
                    method.Temp_ReplaceReturnType(module.FindType(method.ReturnType.Class.FullName, true));
                }

                foreach (var method in @class.Methods)
                {
                    foreach (var argument in method.Signature.Arguments)
                    {
                        if (argument.IsGeneric)
                            continue;
                        if (argument.ComplexType.Class is not UnresolvedVeinClass)
                            continue;
                        argument.Temp_ReplaceType(module.FindType(argument.ComplexType.Class.FullName, true));
                    }
                }
                foreach (var field in @class.Fields)
                {
                    if (field.FieldType.IsGeneric)
                        continue;
                    if (field.FieldType.Class is not UnresolvedVeinClass)
                        continue;
                    field.FieldType = module.FindType(field.FieldType.Class.FullName, true);
                }
            }

            var const_body_len = reader.ReadInt32();
            var const_body = reader.ReadBytes(const_body_len);

            module.const_table = const_body.ToConstStorage();

            module.Name = new ModuleNameSymbol(module.GetConstStringByIndex(idx));
            module.Version = Version.Parse(module.GetConstStringByIndex(vdx));
            module.aspects.AddRange(Aspect.Deconstruct(module.const_table.storage));
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
                                    .Where(method => method.Name.Equals(methodAspect.MethodName))))
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

        public static VeinClass DecodeClass(byte[] arr, ModuleReader module, List<(VeinClass, byte[])> deferMethods)
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


            var @class = new VeinClass(className, parents.ToArray(), module)
            {
                Flags = flags
            };
            
            Debug.Assert(binary.ReadInt32() == 1923);
            var len = binary.ReadInt32();

            foreach (var _ in ..len)
            {
                var body =
                    binary.ReadBytes(binary.ReadInt32());
                deferMethods.Add((@class, body));
                //var method = DecodeMethod(body, @class, module);
                //@class.Methods.Add(method);
            }

            Debug.Assert(binary.ReadInt32() == 8712);

            DecodeField(binary, @class, module);

            return @class;
        }

        public static void DecodeField(BinaryReader binary, VeinClass @class, ModuleReader module)
        {
            foreach (var _ in ..binary.ReadInt32())
            {
                var name = FieldName.Resolve(binary.ReadInt32(), module);
                var type = binary.ReadComplexType(module, false);
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
            var retType = binary.ReadComplexType(module);
            var args = ReadArguments(binary, module);
            var typeArgs = binary.ReadGenericsTypeName(module);
            var _ = binary.ReadBytes(bodysize);
            return new VeinMethod(module.GetConstStringByIndex(idx), flags,
                retType,
                @class, typeArgs, args.ToArray());
        }



        private static List<VeinArgumentRef> ReadArguments(BinaryReader binary, ModuleReader module)
        {
            var args = new List<VeinArgumentRef>();
            foreach (var _ in ..binary.ReadInt32())
            {
                var nIdx = binary.ReadInt32();
                var isGeneric = binary.ReadBoolean();
                if (isGeneric)
                {
                    var generic = binary.ReadGenericTypeName(module);
                    args.Add(new VeinArgumentRef(module.GetConstStringByIndex(nIdx), generic));
                }
                else
                {
                    var type = binary.ReadTypeName(module);
                    args.Add(new VeinArgumentRef(module.GetConstStringByIndex(nIdx), module.FindType(type, true, false)));
                }
            }
            return args;
        }
    }
}
