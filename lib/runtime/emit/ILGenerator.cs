namespace wave.emit
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Text;
    using runtime.emit;

    public class ILGenerator
    {
        private byte[] _ilBody;
        private int _length;
        private readonly MethodBuilder _methodBuilder;
        private readonly StringBuilder _debugBuilder = new ();
        
        public virtual int ILOffset => _length;

        public ILGenerator(MethodBuilder method) : this(method, 64) { }
        public ILGenerator(MethodBuilder method, int size)
        {
            _methodBuilder = method;
            _ilBody = new byte[Math.Max(size, 16)];
        }

        public virtual void Emit(OpCode opcode)
        {
            _debugBuilder.AppendLine(opcode.Name);
            EnsureCapacity(3);
            InternalEmit(opcode);
        }
        public virtual void Emit(OpCode opcode, byte arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.byte");
            EnsureCapacity(4);
            InternalEmit(opcode);
            _ilBody[_length++] = arg;
        }
        public void Emit(OpCode opcode, sbyte arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.sbyte");
            EnsureCapacity(4);
            InternalEmit(opcode);
            _ilBody[_length++] = (byte) arg;
        }
        public virtual void Emit(OpCode opcode, short arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.short");
            EnsureCapacity(5);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt16LittleEndian(_ilBody.AsSpan(_length), arg);
            _length += 2;
        }
        public virtual void Emit(OpCode opcode, long arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.long");
            EnsureCapacity(11);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_length), arg);
            _length += 8;
        }

        public virtual void Emit(OpCode opcode, float arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} {arg}.float");
            EnsureCapacity(7);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_length), BitConverter.SingleToInt32Bits(arg));
            _length += 4;
        }

        public virtual void Emit(OpCode opcode, double arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} {arg}.double");
            EnsureCapacity(11);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_length), BitConverter.DoubleToInt64Bits(arg));
            _length += 8;
        }
        
        public virtual void Emit(OpCode opcode, string str)
        {
            var token = _methodBuilder
                .classBuilder
                .moduleBuilder
                .GetStringConstant(str);
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(token);
            _debugBuilder.AppendLine($"{opcode.Name} '{str}'.0x{token:X8}");
        }


        internal byte[] BakeByteArray()
        {
            if (_length == 0)
                return null;
            return _ilBody;
        }

        internal string BakeDebugString()
        {
            if (_length == 0)
                return "";
            return _debugBuilder.ToString();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutInteger4(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_length), value);
            _length += 4;
        }
        
        internal void InternalEmit(OpCode opcode)
        {
            var num = opcode.Value;
            if (opcode.Size != 1)
            {
                BinaryPrimitives.WriteUInt16BigEndian(_ilBody.AsSpan(_length), num);
                _length += 2;
            }
            else
                _ilBody[_length++] = (byte) num;
            //this.UpdateStackSize(opcode, opcode.StackChange());
        }
        
        
        
        
        internal void EnsureCapacity(int size)
        {
            if (_length + size < _ilBody.Length)
                return;
            IncreaseCapacity(size);
        }
        
        private void IncreaseCapacity(int size)
        {
            var numArray = new byte[Math.Max(_ilBody.Length * 2, _length + size)];
            Array.Copy(_ilBody, numArray, _ilBody.Length);
            _ilBody = numArray;
        }
    }
}