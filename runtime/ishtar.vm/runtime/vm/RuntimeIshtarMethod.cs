namespace ishtar
{
    using System;
    using vein.runtime;

    public unsafe class RuntimeIshtarMethod : VeinMethod
    {
        public MetaMethodHeader Header;
        public PInvokeInfo PIInfo;

        public uint vtable_offset;

        internal RuntimeIshtarMethod(string name, MethodFlags flags, params VeinArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = VeinTypeCode.TYPE_VOID.AsClass();

        internal RuntimeIshtarMethod(string name, MethodFlags flags, VeinClass returnType, params VeinArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = returnType;

        internal RuntimeIshtarMethod(string name, MethodFlags flags, VeinClass returnType, VeinClass owner,
            params VeinArgumentRef[] args)
            : base(name, flags, args)
        {
            this.Owner = owner;
            this.ReturnType = returnType;
        }

        public void SetILCode(uint* code, uint size)
        {
            if ((Flags & MethodFlags.Extern) != 0)
                throw new MethodHasExternException();
            if ((Flags & MethodFlags.Abstract) != 0)
                throw new MethodHasAbstractException();
            Header = new MetaMethodHeader { code = code, code_size = size };
        }

        public void SetExternalLink(void* @ref)
        {
            if ((Flags & MethodFlags.Extern) == 0)
                throw new MethodHasExternException();
            if ((Flags & MethodFlags.Abstract) == 0)
                throw new MethodHasAbstractException();
            PIInfo = new PInvokeInfo { Addr = @ref, iflags = 0 };
        }


        public unsafe RuntimeIshtarMethod AsNative(void* p)
        {
            this.PIInfo = PInvokeInfo.New(p);
            return this;
        }
    }
}
