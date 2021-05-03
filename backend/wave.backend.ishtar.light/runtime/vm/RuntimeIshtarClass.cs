namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using wave.runtime;

    public interface ITransitionAlignment<in TKey, out TValue>
    {
        TValue this[TKey key] { get; }
    }

    public unsafe class RuntimeIshtarClass : WaveClass, 
        ITransitionAlignment<string, RuntimeIshtarField>,
        ITransitionAlignment<string, RuntimeIshtarMethod>
    {
        internal RuntimeIshtarClass(QualityTypeName name, WaveClass parent, WaveModule module)
            : base(name, parent, module) {}
        internal RuntimeIshtarClass(WaveType type, WaveClass parent)
            : base(type, parent) {}


        internal RuntimeIshtarField DefineField(string name, FieldFlags flags, WaveClass type)
        {
            var f = new RuntimeIshtarField(this, new FieldName(name, Name), flags, type);
            this.Fields.Add(f);
            return f;
        }
        
        public ulong computed_size = 0;
        public bool is_inited = false;
        public void** vtable = null;
        public int vtable_size = 0;


        public struct vtable_info
        {
            public void* type_info;
            public void* slot1, slot2;
            public int* flags;
        }

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
            
            computed_size += (ulong)this.Methods.Count * 2;
            computed_size += (ulong)this.Fields.Count * 2;

            if (computed_size >= long.MaxValue) // fuck IntPtr ctor limit
            {
                VM.FastFail(WNE.TYPE_LOAD, $"'{FullName}' too big object.");
                return;
            }

            if (computed_size == 0)
            {
                is_inited = true;
                return;
            }
            vtable = (void**)Marshal.AllocHGlobal(new IntPtr(sizeof(void*) * (long)computed_size));

            if (vtable == null)
            {
                VM.FastFail(WNE.TYPE_LOAD, "Out of memory.");
                return;
            }

            if (p is not null && p.vtable_size != 0)
            {
                Unsafe.CopyBlock(vtable, p.vtable, 
                    (uint)(sizeof(void*) * p.vtable_size));
            }

            var vtable_offset = p?.vtable_size ?? 0;

            for (var i = 0; i != this.Methods.Count; i++, vtable_offset++)
            {
                var method = this.Methods[i] as RuntimeIshtarMethod;
                
                if ((method!.Flags & MethodFlags.Abstract) != 0 && (this.Flags & ClassFlags.Abstract) == 0)
                {
                    VM.FastFail(WNE.TYPE_LOAD,
                        $"Method '{method.Name}' in '{this.Name}' type has invalid mapping.");
                    return;
                }

                vtable[vtable_offset * 2] = IshtarUnsafe.AsPointer(ref method);
                method.vtable_offset = vtable_offset * 2;

                if (p is null)
                    continue;
                
                var w = p.FindMethod(method.Name);
                
                if (w == null && (method.Flags & MethodFlags.Override) != 0)
                    VM.FastFail(WNE.MISSING_METHOD,
                        $"Method '{method.Name}' mark as OVERRIDE," +
                        $" but parent class '{p.Name}' no contained virtual/abstract method.");

                if (w is null)
                    continue;

                if ((method.Flags & MethodFlags.Override) != 0)
                {
                    var tmp = vtable[w.vtable_offset * 2];
                    vtable[w.vtable_offset * 2] = IshtarUnsafe.AsPointer(ref method);
                    vtable[w.vtable_offset * 2 + 1] = tmp;
                }
            }

            if (Fields.Count != 0)
            {
                for (var i = 0; i != Fields.Count; i++, vtable_offset++)
                {
                    var field = Fields[i] as RuntimeIshtarField;

                    if ((field!.Flags & FieldFlags.Abstract) != 0 && (Flags & ClassFlags.Abstract) == 0)
                    {
                        VM.FastFail(WNE.TYPE_LOAD, 
                            $"Field '{field.Name}' in '{this.Name}' type has invalid mapping.");
                        return;
                    }

                    vtable[vtable_offset * 2] = get_field_default_value(field);
                    field.vtable_offset = vtable_offset * 2;

                    if (p is null)
                        continue;

                    var w = p.FindField(field.FullName);

                    if (w == null && (field.Flags & FieldFlags.Override) != 0)
                        VM.FastFail(WNE.MISSING_FIELD, 
                            $"Field '{field.Name}' mark as OVERRIDE," +
                            $" but parent class '{p.Name}' no contained virtual/abstract method.");

                    if (w is null)
                        continue;

                    if ((field.Flags & FieldFlags.Override) != 0)
                    {
                        var tmp = vtable[w.vtable_offset * 2];
                        vtable[w.vtable_offset * 2] = get_field_default_value(w);
                        vtable[w.vtable_offset * 2 + 1] = tmp;
                    }
                }
            }



            is_inited = true;
        }


        public void* get_field_default_value(RuntimeIshtarField field)
        {
            if (field.default_value is not null)
                return field.default_value;
            if (field.FieldType.IsPrimitive)
                return field.default_value = IshtarGC.AllocValue(field.FieldType);
            return null;
        }
        public RuntimeIshtarField FindField(FieldName name)
            => Fields.FirstOrDefault(x => x.FullName.Equals(name)) as RuntimeIshtarField;
        public RuntimeIshtarMethod FindMethod(string fullyName) 
            => Methods.FirstOrDefault(method => method.Name.Equals(fullyName)) as RuntimeIshtarMethod;



        public ITransitionAlignment<string, RuntimeIshtarField> Field => this;
        public ITransitionAlignment<string, RuntimeIshtarMethod> Method => this;

        RuntimeIshtarField ITransitionAlignment<string, RuntimeIshtarField>.this[string key]
        {
            get
            {
                var r = (RuntimeIshtarField) Fields.FirstOrDefault(x => x.Name.Equals(key));

                if (Parent is null && r is null)
                    return null;
                return r ?? (Parent as RuntimeIshtarClass)?.Field[key];
            }
        }

        RuntimeIshtarMethod ITransitionAlignment<string, RuntimeIshtarMethod>.this[string key]
        {
            get
            {
                var r = (RuntimeIshtarMethod) Methods.FirstOrDefault(x => x.Name.Equals(key));

                if (Parent is null && r is null)
                    return null;

                return r ?? (Parent as RuntimeIshtarClass)?.Method[key];
            }
        }
    }
}