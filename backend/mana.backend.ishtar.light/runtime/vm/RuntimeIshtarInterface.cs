namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using mana.extensions;
    using mana.reflection;
    using mana.runtime;
    using MoreLinq;

    public unsafe class RuntimeIshtarInterface : ManaInterface<RuntimeIshtarInterface>,
        ITransitionAlignment<string, RuntimeIshtarField>,
        ITransitionAlignment<string, RuntimeIshtarMethod>,
        IVTableCollectible,
        IDisposable
    {
        internal RuntimeIshtarInterface(QualityTypeName name, IEnumerable<RuntimeIshtarInterface> parents,
            ManaModule module)
            : base(name, parents, module) => this.Flags |= ClassFlags.Abstract;


        internal RuntimeIshtarField DefineField(string name, FieldFlags flags, ManaClass type)
        {
            flags |= FieldFlags.Abstract;

            var f = new RuntimeIshtarField(this, new FieldName(name, Name), flags, type);
            this.Fields.Add(f);
            return f;
        }

        internal RuntimeIshtarMethod DefineMethod(string name, ManaClass returnType, MethodFlags flags, params ManaArgumentRef[] args)
        {
            flags |= MethodFlags.Abstract;

            var method = new RuntimeIshtarMethod(name, flags, returnType, this, args);
            method.Arguments.AddRange(args);

            if (Methods.Any(x => x.Name.Equals(method.Name)))
                throw new Exception();
            
            Methods.Add(method);
            return method;
        }

        public ulong computed_size = 0;
        public bool is_inited = false;
        public void** vtable = null;
        public ulong vtable_size = 0;


        public List<RuntimeIshtarInterface> flatten_parent_map { get; } = new();

        public uint get_vtable_offset(RuntimeIshtarField field)
        {
            var offset = 0U;

            foreach (var @interface in flatten_parent_map)
            {
                var cur = @interface.Field[field.Name];
                if (cur is not null)
                    return (offset + cur.vtable_offset);
                offset += (uint)@interface.vtable_size;
            }

            var m = Field[field.Name];

            if (m is null)
                return uint.MaxValue;
            return (offset + m.vtable_offset) - 1;
        }
        public uint get_vtable_offset(RuntimeIshtarMethod method)
        {
            var offset = 0U;

            foreach (var @interface in flatten_parent_map)
            {
                var cur = @interface.Method[method.Name];
                if (cur is not null)
                    return (offset + cur.vtable_offset);
                offset += (uint)@interface.vtable_size;
            }

            var m = Method[method.Name];

            if (m is null)
                return uint.MaxValue;
            return (offset + m.vtable_offset) - 1;
        }

#if DEBUG_VTABLE
        public debug_vtable dvtable = new ();


        public class debug_vtable
        {
            public object[] vtable = null;
            public ulong vtable_size = 0;
            public ulong computed_size = 0;
        }
#endif
        
        private IEnumerable<RuntimeIshtarInterface> Flatten(IEnumerable<RuntimeIshtarInterface> e) =>
            e.SelectMany(c => Flatten(c.Parents)).Concat(e);

        public void init_vtable()
        {
            if (is_inited)
                return;

            flatten_parent_map.AddRange(Flatten(Parents));

            foreach (var @interface in flatten_parent_map)
            {
                @interface.init_vtable();
                computed_size += @interface.computed_size;
                dvtable.computed_size += @interface.dvtable.computed_size;
            }

            computed_size += (ulong)this.Methods.Count;
            computed_size += (ulong)this.Fields.Count;
            

#if DEBUG_VTABLE
            dvtable.computed_size += (ulong)this.Methods.Count;
            dvtable.computed_size += (ulong)this.Fields.Count;
#endif

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

#if DEBUG_VTABLE
            dvtable.vtable = new object[(long)computed_size];
#endif
            var vtable_parents_offset = 0ul;


            // so, maybe many problems
            for (var i_index = 0; i_index < flatten_parent_map.Count; i_index++)
            {
                var @interface = flatten_parent_map[i_index];
                if (@interface.vtable_size == 0)
                    continue;

                Unsafe.CopyBlock(vtable + vtable_parents_offset, @interface.vtable,
                    (uint)(sizeof(void*) * (uint)@interface.computed_size));
#if DEBUG_VTABLE
                for (var i = 0ul; i != @interface.computed_size; i++)
                    dvtable.vtable[i + (vtable_parents_offset)] = @interface.dvtable.vtable[i];
#endif

                vtable_parents_offset += (@interface.computed_size);
            }

            var vtable_offset = (uint)flatten_parent_map.Sum(x => x.computed_size);

            for (var i = 0; i != this.Methods.Count; i++, vtable_offset++)
            {
                var method = this.Methods[i] as RuntimeIshtarMethod;

                if ((method!.Flags & MethodFlags.Abstract) != 0 && (this.Flags & ClassFlags.Abstract) == 0)
                {
                    VM.FastFail(WNE.TYPE_LOAD,
                        $"Method '{method.Name}' in '{this.Name}' type has invalid mapping.");
                    return;
                }

                vtable[vtable_offset] = IshtarUnsafe.AsPointer(ref method);
                method.vtable_offset = vtable_offset;

#if DEBUG_VTABLE
                dvtable.vtable[vtable_offset] = method;
#endif
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

                    vtable[vtable_offset] = get_field_default_value(field);
                    field.vtable_offset = vtable_offset;

#if DEBUG_VTABLE
                    dvtable.vtable[vtable_offset] = $"DEFAULT_VALUE OF [{field.FullName}::{field.FieldType.Name}]";
#endif
                }
            }

            is_inited = true;
            if (Parents.Count == 0)
            {
                vtable_size = computed_size;
#if DEBUG_VTABLE
                dvtable.vtable_size = computed_size;
#endif
            }
            else
            {
                vtable_size = computed_size - Parents.Sum(x => x.computed_size);
#if DEBUG_VTABLE
                dvtable.vtable_size = dvtable.computed_size - Parents.Sum(x => x.dvtable.computed_size);
#endif
            }
        }

        public void* get_field_default_value(RuntimeIshtarField field)
        {
            if (field.default_value is not null)
                return field.default_value;
            if (field.FieldType.IsPrimitive)
                return field.default_value = IshtarGC.AllocValue(field.FieldType);
            return null;
        }
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

        #region IDisposable

        private void ReleaseUnmanagedResources()
            => Marshal.FreeHGlobal((nint)vtable);

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        ~RuntimeIshtarInterface() => ReleaseUnmanagedResources();

        #endregion
    }
}
