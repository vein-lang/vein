namespace ishtar
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text.Json.Serialization;
    using mana.reflection;
    using mana.runtime;
    using mana.extensions;

    public interface ITransitionAlignment<in TKey, out TValue>
    {
        TValue this[TKey key] { get; }
    }

    public unsafe class RuntimeIshtarClass : ManaClass,
        ITransitionAlignment<string, RuntimeIshtarField>,
        ITransitionAlignment<string, RuntimeIshtarMethod>,
        IVTableCollectible,
        IDisposable
    {
        internal RuntimeIshtarClass(QualityTypeName name, ManaClass parent, RuntimeIshtarModule module)
            : base(name, parent, module)
        {
            ID = module.Vault.TokenGranted.GrantClassID();
            runtime_token = new RuntimeToken(module.ID, ID);
        }

        internal RuntimeIshtarClass(QualityTypeName name, ManaClass[] parents, RuntimeIshtarModule module)
            : base(name, parents, module)
        {
            ID = module.Vault.TokenGranted.GrantClassID();
            runtime_token = new RuntimeToken(module.ID, ID);
        }


        internal RuntimeIshtarField DefineField(string name, FieldFlags flags, ManaClass type)
        {
            var f = new RuntimeIshtarField(this, new FieldName(name, Name), flags, type);
            this.Fields.Add(f);
            return f;
        }

        internal RuntimeIshtarMethod DefineMethod(string name, ManaClass returnType, MethodFlags flags, params ManaArgumentRef[] args)
        {
            var method = new RuntimeIshtarMethod(name, flags, returnType, this, args);
            method.Arguments.AddRange(args);

            if (Methods.Any(x => x.Name.Equals(method.Name)))
                throw new Exception();

            Methods.Add(method);
            return method;
        }

        public RuntimeToken runtime_token { get; }
        public ushort ID { get; }

        public ulong computed_size = 0;
        public bool is_inited = false;
        public void** vtable = null;
        public ulong vtable_size = 0;

#if DEBUG_VTABLE
        public debug_vtable dvtable = new ();


        public class debug_vtable
        {
            public object[] vtable = null;
            public ulong vtable_size = 0;
            public ulong computed_size = 0;
        }
#endif
        public void init_vtable()
        {
            if (is_inited)
                return;
            computed_size = 0;

            var parents = Parents.OfType<RuntimeIshtarClass>().ToArray();

            foreach (var parent in parents)
            {
                parent.init_vtable();
                computed_size += parent.computed_size;
                dvtable.computed_size += parent.dvtable.computed_size;
            }

            computed_size += (ulong)this.Methods.Count;
            computed_size += (ulong)this.Fields.Count;

            //computed_size += 2;

#if DEBUG_VTABLE
            dvtable.computed_size += (ulong)this.Methods.Count;
            dvtable.computed_size += (ulong)this.Fields.Count;

            //dvtable.computed_size += 2;
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
            var vtable_offset = (uint)parents.Sum(x => x.dvtable.computed_size);

            if (parents.Any())
            {
                var offset = 0ul;
                foreach (var p in parents)
                {
                    Unsafe.CopyBlock(vtable + offset, p.vtable,
                        (uint)(sizeof(void*) * (uint)p.vtable_size));
                    offset += p.vtable_size;
                }

#if DEBUG_VTABLE
                var flat = parents.SelectMany(x => x.dvtable.vtable).ToArray();
                for (var i = 0ul; i != vtable_offset; i++)
                    dvtable.vtable[i] = flat[i];
#endif
            }

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

                if (!parents.Any())
                    continue;
                {
                    var w = parents.FirstOrDefault(x => x.FindMethod(method.Name) is not null)
                        ?.Method?[method.Name];

                    if (w == null && (method.Flags & MethodFlags.Override) != 0)
                        VM.FastFail(WNE.MISSING_METHOD,
                            $"Method '{method.Name}' mark as OVERRIDE," +
                            $" but parent class '{parents.Select(x => x.Name).Join(',')}'" +
                            $" no contained virtual/abstract method.");

                    if (w is null)
                        continue;

                    if ((method.Flags & MethodFlags.Override) != 0)
                    {
                        vtable[w.vtable_offset] = vtable[vtable_offset];
#if DEBUG_VTABLE
                        dvtable.vtable[w.vtable_offset] = dvtable.vtable[vtable_offset];
#endif
                    }
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

                    vtable[vtable_offset] = get_field_default_value(field);
                    field.vtable_offset = vtable_offset;

#if DEBUG_VTABLE
                    dvtable.vtable[vtable_offset] = $"DEFAULT_VALUE OF [{field.FullName}::{field.FieldType.Name}]";
#endif

                    if (!parents.Any())
                        continue;

                    {
                        var w = parents
                            .FirstOrDefault(x => x.FindField(field.FullName) is not null)?
                            .Field?[field.FullName];

                        if (w == null && (field.Flags & FieldFlags.Override) != 0)
                            VM.FastFail(WNE.MISSING_FIELD,
                                $"Field '{field.Name}' mark as OVERRIDE," +
                                $" but parent class '{parents.Select(x => x.Name).Join(',')}' " +
                                $"no contained virtual/abstract method.");

                        if (w is null)
                            continue;

                        if ((field.Flags & FieldFlags.Override) != 0)
                        {
                            vtable[w.vtable_offset] = vtable[vtable_offset];
#if DEBUG_VTABLE
                            dvtable.vtable[w.vtable_offset] = dvtable.vtable[vtable_offset];
#endif
                        }
                    }
                }
            }

            if (Fields.Count != 0) for (var i = 0; i != Fields.Count; i++)
                    (Fields[i] as RuntimeIshtarField)?.init_mapping();

            is_inited = true;
            if (!parents.Any())
            {
                vtable_size = computed_size;
#if DEBUG_VTABLE
                dvtable.vtable_size = computed_size;
#endif
            }
            else
            {
                vtable_size = computed_size - parents.Sum(x => x.computed_size);
#if DEBUG_VTABLE
                dvtable.vtable_size = dvtable.computed_size -
                parents.Sum(x => x.dvtable.computed_size);
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
        public new RuntimeIshtarField? FindField(string name)
            => base.FindField(name) as RuntimeIshtarField;
        public RuntimeIshtarMethod? FindMethod(string fullyName)
            => Methods.FirstOrDefault(method => method.Name.Equals(fullyName)) as RuntimeIshtarMethod;



        public ITransitionAlignment<string, RuntimeIshtarField> Field => this;
        public ITransitionAlignment<string, RuntimeIshtarMethod> Method => this;

        RuntimeIshtarField ITransitionAlignment<string, RuntimeIshtarField>.this[string key]
        {
            get
            {
                var r = (RuntimeIshtarField) Fields.FirstOrDefault(x => x.Name.Equals(key));

                if (r is not null)
                    return r;
                if (!Parents.Any())
                    return null;
                foreach (var parent in Parents.OfExactType<RuntimeIshtarClass>())
                {
                    r = parent.Field[key];

                    if (r is not null)
                        return r;
                }

                return null;
            }
        }

        RuntimeIshtarMethod ITransitionAlignment<string, RuntimeIshtarMethod>.this[string key]
        {
            get
            {
                var r = (RuntimeIshtarMethod) Methods.FirstOrDefault(x => x.Name.Equals(key));

                if (r is not null)
                    return r;
                if (!Parents.Any())
                    return null;
                foreach (var parent in Parents.OfExactType<RuntimeIshtarClass>())
                {
                    r = parent.Method[key];

                    if (r is not null)
                        return r;
                }

                return null;
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
        ~RuntimeIshtarClass() => ReleaseUnmanagedResources();

        #endregion
    }
}