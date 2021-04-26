namespace ishtar
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using wave.runtime;

    public unsafe class RuntimeIshtarField : WaveField
    {
        public RuntimeIshtarField(WaveClass owner, FieldName fullName, FieldFlags flags, WaveClass fieldType) : 
            base(owner, fullName, flags, fieldType)
        { }


        public int vtable_offset = 0;
    }

    public unsafe class RuntimeIshtarClass : WaveClass
    {
        internal RuntimeIshtarClass(QualityTypeName name, WaveClass parent, WaveModule module)
            : base(name, parent, module) {}
        internal RuntimeIshtarClass(WaveType type, WaveClass parent)
            : base(type, parent) {}
        
        public long computed_size = 0;
        public bool is_inited = false;
        public void** vtable = null;
        public int vtable_size = 0;
        public void init_vtable()
        {
            if (is_inited)
                return;
            var p = Parent as RuntimeIshtarClass;

            if (p is not null)
            {
                p.init_vtable();
                computed_size += p.computed_size;
            }
            
            computed_size += this.Methods.Count;
            computed_size += this.Fields.Count;

            if (computed_size == 0)
            {
                is_inited = true;
                return;
            }
            vtable = (void**)Marshal.AllocHGlobal(new IntPtr(sizeof(void*) * computed_size));

            if (vtable == null)
            {
                VM.FastFail(WaveNativeException.TYPE_LOAD, "Out of memory.");
                return;
            }

            if (p?.vtable_size != 0)
            {
                Unsafe.CopyBlockUnaligned(vtable, p.vtable, 
                    (uint)(sizeof(void*) * p.vtable_size));
            }

            var vtable_offset = p?.vtable_size ?? 0;

            for (var i = 0; i != this.Methods.Count; i++, vtable_offset++)
            {
                var method = this.Methods[i] as RuntimeIshtarMethod;

                if ((method!.Flags & MethodFlags.Abstract) != 0 && (this.Flags & ClassFlags.Abstract) == 0)
                {
                    VM.FastFail(WaveNativeException.TYPE_LOAD, 
                        $"Method '{method.Name}' in '{this.Name}' type has invalid mapping.");
                    return;
                }

                vtable[vtable_offset] = Unsafe.AsPointer(ref method);
                method.vtable_offset = vtable_offset;
                if (p is null)
                    continue;
                var w = p.FindMethod(method.Name);

                if (w != null && (method.Flags & MethodFlags.Override) != 0)
                    vtable[w.vtable_offset] = Unsafe.AsPointer(ref method);

                if (w == null && (method.Flags & MethodFlags.Abstract) != 0)
                    VM.FastFail(WaveNativeException.MISSING_METHOD, 
                        $"Method '{method.Name}' mark as OVERRIDE," +
                        $" but parent class '{p.Name}' no contained virtual/abstract method.");
                vtable[method.vtable_offset] = Unsafe.AsPointer(ref method);
            }

            if (Fields.Count != 0)
            {
                for (var i = 0; i != Fields.Count; i++, vtable_offset++)
                {
                    var field = Fields[i] as RuntimeIshtarField;

                    if ((field!.Flags & FieldFlags.Abstract) != 0 && (Flags & ClassFlags.Abstract) == 0)
                    {
                        VM.FastFail(WaveNativeException.TYPE_LOAD, 
                            $"Field '{field.Name}' in '{this.Name}' type has invalid mapping.");
                        return;
                    }

                    vtable[vtable_offset] = Unsafe.AsPointer(ref field);
                    field.vtable_offset = vtable_offset;

                    if (p is null)
                        continue;

                    var w = p.FindField(field.FullName);

                    if (w != null && (field.Flags & FieldFlags.Override) != 0)
                        vtable[w.vtable_offset] = Unsafe.AsPointer(ref field); // so it needed?
                    if (w == null && (field.Flags & FieldFlags.Override) != 0)
                        VM.FastFail(WaveNativeException.MISSING_METHOD, 
                            $"Field '{field.Name}' mark as OVERRIDE," +
                            $" but parent class '{p.Name}' no contained virtual/abstract method.");
                    vtable[field.vtable_offset] = vtable;
                }
            }

            is_inited = true;
        }

        public RuntimeIshtarField FindField(FieldName name)
            => Fields.FirstOrDefault(x => x.FullName.Equals(name)) as RuntimeIshtarField;
        public RuntimeIshtarMethod FindMethod(string fullyName) 
            => Methods.FirstOrDefault(method => method.Name.Equals(fullyName)) as RuntimeIshtarMethod;
    }

    public unsafe class RuntimeIshtarMethod : WaveMethod
    {
        public MetaMethodHeader Header;
        public WaveMethodPInvokeInfo PIInfo;

        public int vtable_offset;

        internal RuntimeIshtarMethod(string name, MethodFlags flags, params WaveArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = WaveTypeCode.TYPE_VOID.AsClass();

        internal RuntimeIshtarMethod(string name, MethodFlags flags, WaveClass returnType, WaveClass owner,
            params WaveArgumentRef[] args)
            : base(name, flags, args)
        {
            this.Owner = owner;
            this.ReturnType = returnType;
        }

        public void SetILCode(uint* code, uint size)
        {
            if ((Flags & MethodFlags.Extern) != 0)
                throw new MethodHasExternException();
            Header = new MetaMethodHeader { code = code, code_size = size };
        }

        public void SetExternalLink(void* @ref)
        {
            if ((Flags & MethodFlags.Extern) == 0)
                throw new InvalidOperationException("Cannot set native reference, method is not extern.");
            PIInfo = new WaveMethodPInvokeInfo { Addr = @ref, iflags = 0 };
        }
    }
}