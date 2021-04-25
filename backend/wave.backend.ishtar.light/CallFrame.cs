namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using wave.runtime;
    public class MethodHasExternException : Exception {}
    public struct ILLabel
    {
        public OpCodeValue opcode;
        public int pos;
    };

    public unsafe struct WaveMethodPInvokeInfo
    {
        public ushort iflags;
        public void* Addr;
    }
    public unsafe struct MetaMethodHeader
    {
        public uint                         code_size;
        public uint*                        code;
        public short                        max_stack;
        public uint                         local_var_sig_tok;
        public uint                         init_locals;
        public void*                        exception_handler_list;
        public Dictionary<int, ILLabel>     labels_map;
        public List<int>                    labels;
    }
    public unsafe class RuntimeWaveMethod : WaveMethod
    {
        public MetaMethodHeader Header;
        public WaveMethodPInvokeInfo PIInfo;

        public int vtable_offset;

        internal RuntimeWaveMethod(string name, MethodFlags flags, params WaveArgumentRef[] args)
            : base(name, flags, args) =>
            this.ReturnType = WaveTypeCode.TYPE_VOID.AsType();

        internal RuntimeWaveMethod(string name, MethodFlags flags, WaveType returnType, WaveClass owner,
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

    public unsafe class CallFrame
    {
        public CallFrame parent;
        public RuntimeWaveMethod method;
        public stackval* returnValue;
        public void* _this_;
        public stackval* args;
        public stackval* stack;
        public int level;

        public CallFrameException exception;


        public static void FillStackTrace(CallFrame frame)
        {
            var str = new StringBuilder();

            str.AppendLine($"\tat {frame.method.Owner.FullName.NameWithNS}.{frame.method.Name}");
            
            var r = frame.parent;

            while (r != null)
            {
                str.AppendLine($"\tat {frame.method.Owner.FullName.NameWithNS}.{frame.method.Name}");
                r = r.parent;
            }
            frame.exception.stack_trace = str.ToString();
        }
    }

    public unsafe class CallFrameException
    {
        public uint* last_ip;
        public WaveObject value;
        public string stack_trace;
    };
}