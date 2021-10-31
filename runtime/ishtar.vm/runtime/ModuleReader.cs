namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using vein.exceptions;
    using vein.extensions;
    using emit;
    using emit.extensions;
    using MoreLinq;
    using vein.reflection;
    using vein.runtime;

    public class RuntimeIshtarModule : VeinModule
    {
        public AppVault Vault { get; }
        public ushort ID { get; internal set; }

        public RuntimeIshtarClass FindType(RuntimeToken type,
            bool findExternally = false)
        {
            var result = class_table.OfExactType<RuntimeIshtarClass>().FirstOrDefault(filter);
            if (result is not null)
                return result;

            bool filter(RuntimeIshtarClass x) => x!.runtime_token.Equals(type);

            if (!findExternally)
                return null;

            foreach (var module in Deps.OfExactType<RuntimeIshtarModule>())
            {
                result = module.FindType(type, true);
                if (result is not null)
                    return result;
            }

            return null;
        }


        public static RuntimeIshtarModule Read(AppVault vault, byte[] arr, IReadOnlyList<VeinModule> deps, Func<string, Version, VeinModule> resolver)
        {
            var module = new RuntimeIshtarModule(vault);
            using var mem = new MemoryStream(arr);
            using var reader = new BinaryReader(mem);
            module.Deps.AddRange(deps);

            var idx = reader.ReadInt32(); // name index
            var vdx = reader.ReadInt32(); // version index
            var ilVersion = reader.ReadInt32();

            if (ilVersion != OpCodes.SetVersion)
            {
                var exp = new ILCompatibleException(ilVersion, OpCodes.SetVersion);

                VM.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, $"Unable to load assembly: '{exp.Message}'.");
                VM.ValidateLastError();
                return null;
            }

            // read strings table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var value = reader.ReadVeinString();
                module.strings_table.Add(key, value);
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
                module.class_table.Add(@class);
                if (@class.IsSpecial)
                {
                    if (VeinCore.All.Any(x => x.FullName == @class.FullName))
                        TypeForwarder.Indicate(@class);
                }
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
            module.aspects.AddRange(Aspect.Deconstruct(module.const_table.storage));

            DistributionAspects(module);
            ValidateRuntimeTokens(module);
            LinkFFIMethods(module);

            return module;
        }
        [Conditional("VALIDATE_RUNTIME_TOKEN")]
        public static void ValidateRuntimeTokens(RuntimeIshtarModule module)
        {
            foreach (var @class in module.class_table.OfType<RuntimeIshtarClass>())
            {
                VM.Assert(@class.runtime_token != RuntimeToken.Default, WNE.TYPE_LOAD,
                    $"Detected non-inited runtime token. type: '{@class.FullName.NameWithNS}'");
            }
        }

        // shit, todo: refactoring
        public static void DistributionAspects(RuntimeIshtarModule ishtarModule)
        {
            foreach (var aspect in ishtarModule.aspects)
            {
                switch (aspect)
                {
                    case AspectOfClass classAspect:
                        {
                            foreach (var @class in ishtarModule.class_table
                                .Where(@class => @class.Name.Equals(classAspect.ClassName)))
                                @class.Aspects.Add(aspect);
                            break;
                        }
                    case AspectOfMethod methodAspect:
                        {
                            foreach (var method in ishtarModule.class_table
                                .Where(@class => @class.Name.Equals(methodAspect.ClassName))
                                .SelectMany(@class => @class.Methods
                                    .Where(method => method.RawName.Equals(methodAspect.MethodName))))
                                method.Aspects.Add(aspect);
                            break;
                        }
                    case AspectOfField fieldAspect:
                        {
                            foreach (var field in ishtarModule.class_table
                                .Where(@class => @class.Name.Equals(fieldAspect.ClassName))
                                .SelectMany(@class => @class.Fields
                                    .Where(field => field.Name.Equals(fieldAspect.FieldName))))
                                field.Aspects.Add(aspect);
                            break;
                        }
                }
            }
        }

        public static RuntimeIshtarClass DecodeClass(byte[] arr, RuntimeIshtarModule ishtarModule)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var className = binary.ReadTypeName(ishtarModule);
            var flags = (ClassFlags)binary.ReadInt16();

            var parentLen = binary.ReadInt16();

            var parents = new List<VeinClass>();
            foreach (var _ in ..parentLen)
            {
                var parentIdx = binary.ReadTypeName(ishtarModule);
                parents.Add(ishtarModule.FindType(parentIdx, true, false));
            }


            var len = binary.ReadInt32();

            var @class = new RuntimeIshtarClass(className, parents.ToArray()
                , ishtarModule)
            {
                Flags = flags
            };

            foreach (var _ in ..len)
            {
                var body =
                    binary.ReadBytes(binary.ReadInt32());
                var method = DecodeMethod(body, @class, ishtarModule);
                @class.Methods.Add(method);
            }

            DecodeField(binary, @class, ishtarModule);

            return @class;
        }

        public static void DecodeField(BinaryReader binary, VeinClass @class, RuntimeIshtarModule ishtarModule)
        {
            foreach (var _ in ..binary.ReadInt32())
            {
                var name = FieldName.Resolve(binary.ReadInt32(), ishtarModule);
                var type_name = binary.ReadTypeName(ishtarModule);
                var type = ishtarModule.FindType(type_name, true, false);
                var flags = (FieldFlags) binary.ReadInt16();
                var method = new RuntimeIshtarField(@class, name, flags, type);
                @class.Fields.Add(method);
            }
        }

        public static unsafe RuntimeIshtarMethod DecodeMethod(byte[] arr, VeinClass @class, RuntimeIshtarModule ishtarModule)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var idx = binary.ReadInt32();
            var flags = (MethodFlags)binary.ReadInt16();
            var bodysize = binary.ReadInt32();
            var stacksize = binary.ReadByte();
            var locals = binary.ReadByte();
            var retType = binary.ReadTypeName(ishtarModule);
            var args = ReadArguments(binary, ishtarModule);
            var body = binary.ReadBytes(bodysize);


            var mth = new RuntimeIshtarMethod(ishtarModule.GetConstStringByIndex(idx), flags,
                ishtarModule.FindType(retType, true, false),
                @class, args.ToArray());

            if (mth.IsExtern)
                return mth;

            ConstructIL(mth, body, stacksize);

            return mth;
        }

        public static unsafe void LinkFFIMethods(VeinModule module) =>
            module.class_table
                .Select(x => x.Methods.OfExactType<RuntimeIshtarMethod>())
                .Pipe(LinkFFIMethods)
                .Consume();

        public static unsafe void LinkFFIMethods(IEnumerable<RuntimeIshtarMethod> methods)
        {
            foreach (var method in methods.Where(x => x.IsExtern))
            {
                var aspect = method.Aspects.FirstOrDefault(x => x.Name.Equals("Native"));
                var name = method.Name;
                if (aspect is not null)
                {
                    if (aspect.Arguments.Count != 1)
                    {
                        VM.FastFail(WNE.TYPE_LOAD, $"(0x1) Native aspect incorrect arguments. [{method.Name}]");
                        VM.ValidateLastError();
                        return;
                    }

                    if (aspect.Arguments[0].Value is not string s)
                    {
                        VM.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{method.Name}]");
                        VM.ValidateLastError();
                        return;
                    }

                    name = VeinMethodBase.GetFullName(s, method.Arguments);
                }

                var m = FFI.GetMethod(name);

                if (m is null)
                {
                    VM.FastFail(WNE.TYPE_LOAD, $"Extern '{method.Name} -> {name}' method not found in native mapping.");
                    VM.ValidateLastError();
                    return;
                }

                method.PIInfo = m.PIInfo;
            }
        }


        internal static unsafe void ConstructIL(RuntimeIshtarMethod method, byte[] body, short stacksize)
        {
            var offset = 0;
            var body_r = ILReader.Deconstruct(body, &offset, method);
            var labels = ILReader.DeconstructLabels(body, offset);


            method.Header.max_stack = stacksize;

            method.Header.code = (uint*)Marshal.AllocHGlobal(sizeof(uint) * body_r.opcodes.Count);

            for (var i = 0; i != body_r.opcodes.Count; i++)
                method.Header.code[i] = body_r.opcodes[i];


            method.Header.code_size = (uint)body_r.opcodes.Count;
            method.Header.labels = labels;
            method.Header.labels_map = body_r.map.ToDictionary(x => x.Key,
                x => new ILLabel
                {
                    opcode = x.Value.opcode,
                    pos = x.Value.pos
                });
        }


        private static List<VeinArgumentRef> ReadArguments(BinaryReader binary, RuntimeIshtarModule ishtarModule)
        {
            var args = new List<VeinArgumentRef>();
            foreach (var _ in ..binary.ReadInt32())
            {
                var nIdx = binary.ReadInt32();
                var type = binary.ReadTypeName(ishtarModule);
                args.Add(new VeinArgumentRef
                {
                    Name = ishtarModule.GetConstStringByIndex(nIdx),
                    Type = ishtarModule.FindType(type, true, false)
                });
            }
            return args;
        }
        public RuntimeIshtarModule(AppVault vault) : base(null) => Vault = vault;
        public RuntimeIshtarModule(AppVault vault, string name) : base(name) => Vault = vault;
    }
}
