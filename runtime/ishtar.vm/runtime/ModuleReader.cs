namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using vein.exceptions;
    using vein.extensions;
    using emit;
    using emit.extensions;
    using MoreLinq;
    using vein.reflection;
    using vein.runtime;
    using vm;

    public class RuntimeIshtarModule : VeinModule
    {
        public AppVault Vault { get; }
        public VirtualMachine vm => Vault.vm;
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
            var module = new RuntimeIshtarModule(vault, "unnamed");
            using var mem = new MemoryStream(arr);
            using var reader = new BinaryReader(mem);
            module.Deps.AddRange(deps);

            var idx = reader.ReadInt32(); // name index
            var vdx = reader.ReadInt32(); // version index
            var ilVersion = reader.ReadInt32();

            if (ilVersion != OpCodes.SetVersion)
            {
                var exp = new ILCompatibleException(ilVersion, OpCodes.SetVersion);

                vault.vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, $"Unable to load assembly: '{exp.Message}'.", module.sys_frame);
                return null;
            }

            // read strings table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var value = reader.ReadIshtarString();
                module.strings_table.Add(key, value);
            }
            // read types table
            foreach (var _ in ..reader.ReadInt32())
            {
                var key = reader.ReadInt32();
                var asmName = reader.ReadIshtarString();
                var ns = reader.ReadIshtarString();
                var name = reader.ReadIshtarString();
                module.types_table.Add(key, new QualityTypeName(asmName, name, ns));
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
            // read class storage
            foreach (var _ in ..reader.ReadInt32())
            {
                var body = reader.ReadBytes(reader.ReadInt32());
                var @class = module.DecodeClass(body, module);
                module.class_table.Add(@class);
                if (@class.IsSpecial)
                {
                    if (vault.vm.Types.All.Any(x => x.FullName == @class.FullName))
                        TypeForwarder.Indicate(vault.vm.Types, @class);
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

            module.SetupBootstrapper(vault);

            DistributionAspects(module);
            ValidateRuntimeTokens(module);
            module.LinkFFIMethods(module);
            InitVTables(module);

            return module;
        }
        [Conditional("VALIDATE_RUNTIME_TOKEN")]
        public static void ValidateRuntimeTokens(RuntimeIshtarModule module)
        {
            foreach (var @class in module.class_table.OfType<RuntimeIshtarClass>())
            {
                VirtualMachine.Assert(@class.runtime_token != RuntimeToken.Default, WNE.TYPE_LOAD,
                    $"Detected non-inited runtime token. type: '{@class.FullName.NameWithNS}'");
            }
        }

        public static void InitVTables(RuntimeIshtarModule ishtarModule)
            => ishtarModule.class_table.OfType<RuntimeIshtarClass>().Pipe(x => x.init_vtable(ishtarModule.vm)).Consume();

        // shit, todo: refactoring
        public static void DistributionAspects(RuntimeIshtarModule module)
        {
            var errors = new StringBuilder();
            var classes = module.class_table;

            var class_eq = (VeinClass x, string clazz) => x.Name.Equals(clazz);

            foreach (var aspect in module.aspects)
            {
                switch (aspect)
                {
                    case AspectOfClass classAspect:
                        {
                            var @class = classes.FirstOrDefault(x => class_eq(x, classAspect.ClassName));
                            if (@class is not null)
                                @class.Aspects.Add(aspect);
                            else
                                errors.AppendLine($"Aspect '{classAspect.Name}': class '{classAspect.ClassName}' not found.");
                            break;
                        }
                    case AspectOfMethod ma:
                        {
                            var method = classes
                            .Where(x => class_eq(x, ma.ClassName))
                            .SelectMany(x => x.Methods)
                            .FirstOrDefault(method => method.Name.Equals(ma.MethodName));
                            if (method is not null)
                                method.Aspects.Add(aspect);
                            else
                                errors.AppendLine($"Aspect '{ma.Name}': method '{ma.ClassName}/{ma.MethodName}' not found.");
                            break;
                        }
                    case AspectOfField fa when !fa.IsNative(): // currently ignoring native aspect, todo
                        {
                            var field = classes
                            .Where(x => class_eq(x, fa.ClassName))
                            .SelectMany(@class => @class.Fields)
                            .FirstOrDefault(field => field.Name.Equals(fa.FieldName));
                            if (field is not null)
                                field.Aspects.Add(aspect);
                            else
                                errors.AppendLine($"Aspect '{fa.Name}': field '{fa.ClassName}/{fa.FieldName}' not found.");
                            break;
                        }
                }
            }

            if (errors.Length != 0)
                module.Vault.vm.FastFail(WNE.TYPE_LOAD, $"\n{errors}", module.sys_frame);
        }

        public RuntimeIshtarClass DecodeClass(byte[] arr, RuntimeIshtarModule ishtarModule)
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
                DecodeAndDefineMethod(body, @class, ishtarModule);
            }

            DecodeField(binary, @class, ishtarModule);

            return @class;
        }

        public void DecodeField(BinaryReader binary, VeinClass @class, RuntimeIshtarModule ishtarModule)
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

        public unsafe RuntimeIshtarMethod DecodeAndDefineMethod(byte[] arr, RuntimeIshtarClass @class, RuntimeIshtarModule ishtarModule)
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

            var methodName = ishtarModule.GetConstStringByIndex(idx);
            var returnType = ishtarModule.FindType(retType, true, false);

            var mth = @class.DefineMethod(methodName, returnType, flags, args.ToArray());

            if (mth.IsExtern)
                return mth;

            ConstructIL(mth, body, stacksize, ishtarModule);

            return mth;
        }

        public void LinkFFIMethods(VeinModule module) =>
            module.class_table
                .Select(x => x.Methods.OfExactType<RuntimeIshtarMethod>())
                .Pipe(LinkFFIMethods)
                .Consume();

        public void LinkFFIMethods(IEnumerable<RuntimeIshtarMethod> methods)
        {
            const string InternalTarget = "__internal__";
            foreach (var method in methods.Where(x => x.IsExtern))
            {
                var aspect = method.Aspects.FirstOrDefault(x => x.IsNative());
                var name = method.Name;
                if (aspect is null)
                {
                    vm.FastFail(WNE.TYPE_LOAD, $"(0x1) Extern function without native aspect. [{method.Name}]", sys_frame);
                    return;
                }

                if (aspect.Arguments.Count != 2)
                {
                    vm.FastFail(WNE.TYPE_LOAD, $"(0x1) Native aspect incorrect arguments. [{method.Name}]", sys_frame);
                    return;
                }

                if (aspect.Arguments[0].Value is not string importTarget)
                {
                    vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{method.Name}]", sys_frame);
                    return;
                }

                if (aspect.Arguments[1].Value is not string importFn)
                {
                    vm.FastFail(WNE.TYPE_LOAD, $"(0x2) Native aspect incorrect arguments. [{method.Name}]", sys_frame);
                    return;
                }

                if (importTarget == InternalTarget)
                {
                    name = VeinMethodBase.GetFullName(importFn, method.Arguments);
                    LinkInternalNative(name, method);
                    continue;
                }

                ForeignFunctionInterface.LinkExternalNativeLibrary(importTarget, importFn, method);
            }
        }
        private unsafe void LinkInternalNative(string name, RuntimeIshtarMethod method)
        {
            var m = vm.FFI.GetMethod(name);

            if (m is null)
            {
                if (method.Name != name)
                    vm.FastFail(WNE.TYPE_LOAD, $"Extern '{method.Name} -> {name}' method not found in native mapping.", sys_frame);
                else
                    vm.FastFail(WNE.TYPE_LOAD, $"Extern '{method.Name}' method not found in native mapping.", sys_frame);

                vm.FFI.DisplayDefinedMapping();
                return;
            }

            if (m.PIInfo is { Addr: null, iflags: 0 })
            {
                vm.FastFail(WNE.TYPE_LOAD, $"Extern '{method.Name}' method has nul PIInfo", sys_frame);
                vm.FFI.DisplayDefinedMapping();
                return;
            }

            method.PIInfo = m.PIInfo;
        }


        internal static unsafe void ConstructIL(RuntimeIshtarMethod method, byte[] body, short stacksize, RuntimeIshtarModule module)
        {
            var offset = 0;
            var body_r = ILReader.Deconstruct(body, &offset, method);
            var labels = ILReader.DeconstructLabels(body, &offset);
            var exceptions = ILReader.DeconstructExceptions(body, offset, module);


            method.Header.max_stack = stacksize;
            method.Header.exception_handler_list = exceptions;

            method.Header.code = (uint*)NativeMemory.AllocZeroed((nuint)(sizeof(uint) * body_r.opcodes.Count));

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
        public RuntimeIshtarModule(AppVault vault, string name) : base(name, vault.vm.Types) => Vault = vault;

        private void SetupBootstrapper(AppVault vault) =>
            Bootstrapper = new RuntimeIshtarClass(new QualityTypeName(Name, "boot", "<sys>"), Array.Empty<VeinClass>(), this);
        public RuntimeIshtarClass Bootstrapper { get; private set; }

        public CallFrame sys_frame => Vault.vm.Frames.ModuleLoaderFrame;
    }
}
