namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using wave.extensions;
    using wave.ishtar.emit;
    using wave.ishtar.emit.extensions;
    using wave.runtime;
    internal class RuntimeModuleReader : WaveModule
    {

        public static RuntimeModuleReader Read(byte[] arr, List<WaveModule> deps, Func<string, Version, WaveModule> resolver)
        {
            var module = new RuntimeModuleReader();
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
                module.class_table.Add(@class);
            }

            var const_body_len = reader.ReadInt32();
            var const_body = reader.ReadBytes(const_body_len);

            module.Name = module.GetConstStringByIndex(idx);
            module.Version = Version.Parse(module.GetConstStringByIndex(vdx));

            return module;
        }


        public static RuntimeIshtarClass DecodeClass(byte[] arr, RuntimeModuleReader module)
        {
            using var mem = new MemoryStream(arr);
            using var binary = new BinaryReader(mem);
            var className = binary.ReadTypeName(module);
            var flags = (ClassFlags)binary.ReadInt16();
            var parentIdx = binary.ReadTypeName(module);
            var len = binary.ReadInt32();

            var parent = module.FindType(parentIdx, true);
            var @class = new RuntimeIshtarClass(className, parent, module)
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
        
        public static void DecodeField(BinaryReader binary, WaveClass @class, RuntimeModuleReader module)
        {
            foreach (var _ in ..binary.ReadInt32())
            {
                var name = FieldName.Resolve(binary.ReadInt32(), module);
                var type_name = binary.ReadTypeName(module);
                var type = module.FindType(type_name, true);
                var flags = (FieldFlags) binary.ReadInt16();
                var method = new RuntimeIshtarField(@class, name, flags, type);
                @class.Fields.Add(method);
            }
        }
        
        public static unsafe RuntimeIshtarMethod DecodeMethod(byte[] arr, WaveClass @class, RuntimeModuleReader module)
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
            var body = binary.ReadBytes(bodysize);

            
            var mth = new RuntimeIshtarMethod(module.GetConstStringByIndex(idx), flags,
                module.FindType(retType, true), 
                @class, args.ToArray());

            if (flags.HasFlag(MethodFlags.Extern))
            {
                var m = FFI.GetMethod(mth.Name);

                if (m is null)
                {
                    VM.FastFail(WNE.TYPE_LOAD, $"Extern '{mth.Name}' method not found in native mapping.");
                    VM.ValidateLastError();
                    return null;
                }

                mth.PIInfo = m.PIInfo;

                return mth;
            }

            
            var offset = 0;
            var body_r = ILReader.Deconstruct(body, &offset);
            var labeles = ILReader.DeconstructLabels(body, offset);
            

            mth.Header.max_stack = stacksize;

            mth.Header.code = (uint*)Marshal.AllocHGlobal(sizeof(uint) * body_r.opcodes.Count);
            
            for (var i = 0; i != body_r.opcodes.Count; i++)
                mth.Header.code[i] = body_r.opcodes[i];


            mth.Header.code_size = (uint) body_r.opcodes.Count;
            mth.Header.labels = labeles;
            mth.Header.labels_map = body_r.map.ToDictionary(x => x.Key,
                x => new ILLabel
                {
                    opcode = x.Value.opcode,
                    pos = x.Value.pos
                });
            return mth;
        }

        
        
        private static List<WaveArgumentRef> ReadArguments(BinaryReader binary, RuntimeModuleReader module)
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
        public RuntimeModuleReader() : base(null)
        {
        }
    }
}