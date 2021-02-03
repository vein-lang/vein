namespace wave.emit
{
    using System;
    using System.Buffers.Binary;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using extensions;
    using runtime.emit;
    using static OpCodeValue;

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

        public MethodBuilder GetMethodBuilder() => _methodBuilder;

        public virtual void Emit(OpCode opcode)
        {
            _debugBuilder.AppendLine(opcode.Name);
            EnsureCapacity<OpCode>(sizeof(byte));
            InternalEmit(opcode);
        }
        public virtual void Emit(OpCode opcode, byte arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.byte");
            EnsureCapacity<OpCode>(sizeof(byte));
            InternalEmit(opcode);
            _ilBody[_length++] = arg;
        }
        public void Emit(OpCode opcode, sbyte arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.sbyte");
            EnsureCapacity<OpCode>(sizeof(sbyte));
            InternalEmit(opcode);
            _ilBody[_length++] = (byte) arg;
        }
        public virtual void Emit(OpCode opcode, short arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.short");
            EnsureCapacity<OpCode>(sizeof(short));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt16LittleEndian(_ilBody.AsSpan(_length), arg);
            _length += sizeof(short);
        }
        
        public virtual void Emit(OpCode opcode, int arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.int");
            EnsureCapacity<OpCode>(sizeof(int));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_length), arg);
            _length += sizeof(int);
        }
        public virtual void Emit(OpCode opcode, long arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} 0x{arg:X8}.long");
            EnsureCapacity<OpCode>(sizeof(long));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_length), arg);
            _length += sizeof(long);
        }

        public virtual void Emit(OpCode opcode, float arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} {arg}.float");
            EnsureCapacity<OpCode>(sizeof(float));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_length), BitConverter.SingleToInt32Bits(arg));
            _length += sizeof(float);
        }

        public virtual void Emit(OpCode opcode, double arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} {arg}.double");
            EnsureCapacity<OpCode>(sizeof(double));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_length), BitConverter.DoubleToInt64Bits(arg));
            _length += sizeof(double);
        }
        
        public virtual void Emit(OpCode opcode, decimal arg)
        {
            _debugBuilder.AppendLine($"{opcode.Name} {arg}.decimal");
            EnsureCapacity<OpCode>(sizeof(decimal));
            InternalEmit(opcode);
            foreach (var i in decimal.GetBits(arg))
                BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_length), i);
            _length += sizeof(decimal);
        }
        
        public virtual void Emit(OpCode opcode, string str)
        {
            var token = _methodBuilder
                .classBuilder
                .moduleBuilder
                .GetStringConstant(str);
            this.EnsureCapacity<OpCode>(sizeof(int));
            InternalEmit(opcode);
            PutInteger4(token);
            _debugBuilder.AppendLine($"{opcode.Name} '{str}'.0x{token:X8}");
        }

        public virtual void Emit(OpCode opcode, FieldName field)
        {
            if (opcode.Value != (int) LDF)
                throw new Exception("invalid opcode");
            
            var (token, direction) = this.FindFieldToken(field);

            opcode = direction switch
            {
                FieldDirection.Local => this._methodBuilder.GetLocalIndex(field) switch
                {
                    0 => OpCodes.LDLOC_0,
                    1 => OpCodes.LDLOC_1,
                    2 => OpCodes.LDLOC_2,
                    3 => OpCodes.LDLOC_3,
                    4 => OpCodes.LDLOC_4,
                    _ => throw new Exception(/* todo */)
                },
                FieldDirection.Arg => this._methodBuilder.GetArgumentIndex(field) switch
                {
                    0 => OpCodes.LDARG_0,
                    1 => OpCodes.LDARG_1,
                    2 => OpCodes.LDARG_2,
                    3 => OpCodes.LDARG_3,
                    4 => OpCodes.LDARG_4,
                    _ => throw new Exception(/* todo */)
                },
                FieldDirection.Member => throw new Exception(/* todo */),
                _ => throw new Exception(/* todo */)
            };
            this.EnsureCapacity<OpCode>();
            this.InternalEmit(opcode);
        }
        
        public virtual void EmitCall(OpCode opcode, WaveClassMethod method)
        {
            if (method is null)
                throw new ArgumentNullException(nameof (method));
            var token = this._methodBuilder.classBuilder.moduleBuilder.GetMethodToken(method);
            this.EnsureCapacity<OpCode>(sizeof(int));
            this.InternalEmit(opcode);
            this.PutInteger4(token);
        }
        
        public enum FieldDirection
        {
            Arg,
            Local,
            Member
        }
        
        
        private (ulong, FieldDirection) FindFieldToken(FieldName field)
        {
            var token = (ulong?)0;
            if ((token = this._methodBuilder.FindArgumentField(field)) != null)
                return (token.Value, FieldDirection.Arg);
            if ((token = this._methodBuilder.FindLocalField(field)) != null)
                return (token.Value, FieldDirection.Local);
            if ((token = this._methodBuilder.classBuilder.FindMemberField(field)) != null)
                return (token.Value, FieldDirection.Member);
            throw new Exception($"Field '{field}' is not declared."); // TODO custom exception
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
            _length += sizeof(int);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutUInteger8(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_ilBody.AsSpan(_length), value);
            _length += sizeof(ulong);
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
        internal void EnsureCapacity<T>(params int[] sizes) where T : struct
        {
            var sum = sizes.Sum() + sizeof(ushort);
            EnsureCapacity(sum);
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