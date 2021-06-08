namespace mana.ishtar.emit
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using extensions;
    using global::ishtar;
    using mana.runtime;
    using static global::ishtar.OpCodeValue;

    public class ILGenerator
    {
        private byte[] _ilBody;
        private int _position;
        internal List<ushort> _opcodes = new List<ushort>();
        internal readonly MethodBuilder _methodBuilder;
        private readonly StringBuilder _debugBuilder = new ();

        internal int LocalsSize { get; set; }

        public virtual int ILOffset => _position;

        internal ILGenerator(MethodBuilder method) : this(method, 16) { }
        internal ILGenerator(MethodBuilder method, int size)
        {
            _methodBuilder = method;
            _ilBody = new byte[Math.Max(size, 16)];
        }

        internal MethodBuilder GetMethodBuilder() => _methodBuilder;

        public virtual void Emit(OpCode opcode)
        {
            EnsureCapacity<OpCode>();
            InternalEmit(opcode);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name}");
        }
        public virtual void Emit(OpCode opcode, byte arg)
        {
            EnsureCapacity<OpCode>(sizeof(byte));
            InternalEmit(opcode);
            _ilBody[_position++] = arg;
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.byte");
        }
        public void Emit(OpCode opcode, sbyte arg)
        {
            EnsureCapacity<OpCode>(sizeof(sbyte));
            InternalEmit(opcode);
            _ilBody[_position++] = (byte)arg;
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.sbyte");
        }
        public virtual void Emit(OpCode opcode, short arg)
        {
            EnsureCapacity<OpCode>(sizeof(short));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt16LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(short);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.short");
        }

        public virtual void Emit(OpCode opcode, int arg)
        {
            EnsureCapacity<OpCode>(sizeof(int));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(int);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.int");
        }
        public virtual void Emit(OpCode opcode, long arg)
        {
            EnsureCapacity<OpCode>(sizeof(long));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(long);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.long");
        }

        public virtual void Emit(OpCode opcode, ulong arg)
        {
            EnsureCapacity<OpCode>(sizeof(ulong));
            InternalEmit(opcode);
            BinaryPrimitives.WriteUInt64LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(ulong);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.ulong");
        }

        public virtual void Emit(OpCode opcode, float arg)
        {
            EnsureCapacity<OpCode>(sizeof(float));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), BitConverter.SingleToInt32Bits(arg));
            _position += sizeof(float);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {arg}.float");
        }

        public virtual void Emit(OpCode opcode, double arg)
        {
            EnsureCapacity<OpCode>(sizeof(double));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_position), BitConverter.DoubleToInt64Bits(arg));
            _position += sizeof(double);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {arg}.double");
        }

        public virtual void Emit(OpCode opcode, decimal arg)
        {
            EnsureCapacity<OpCode>(sizeof(decimal));
            InternalEmit(opcode);
            var bits = decimal.GetBits(arg);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), bits.Length);
            _position += sizeof(int);
            foreach (var i in bits)
            {
                BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), i);
                _position += sizeof(int);
            }
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {arg}.decimal");
        }
        /// <remarks>
        /// <see cref="string"/> will be interned.
        /// </remarks>
        public virtual void Emit(OpCode opcode, string str)
        {
            var token = _methodBuilder
                .classBuilder
                .moduleBuilder
                .InternString(str);
            this.EnsureCapacity<OpCode>(sizeof(int));
            InternalEmit(opcode);
            PutInteger4(token);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} .0x{token:X8}");
        }
        /// <summary>
        /// Emit opcodes for stage\load value for field.
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="field"></param>
        /// <exception cref="InvalidOpCodeException"></exception>
        /// <remarks>
        /// Only allowed <see cref="OpCodes.LDF"/>, <see cref="OpCodes.STF"/>,
        /// <see cref="OpCodes.STSF"/>, <see cref="OpCodes.LDSF"/>.
        /// </remarks>
        public virtual void Emit(OpCode opcode, ManaField field)
        {
            if (new[] { OpCodes.LDF, OpCodes.STF, OpCodes.STSF, OpCodes.LDSF }.All(x => x != opcode))
                throw new InvalidOpCodeException($"Opcode '{opcode.Name}' is not allowed.");

            this.EnsureCapacity<OpCode>(sizeof(int) * 2);
            this.InternalEmit(opcode);

            var nameIdx = this._methodBuilder.moduleBuilder.InternFieldName(field.FullName);
            var typeIdx = this._methodBuilder.moduleBuilder.InternTypeName(field.Owner.FullName);

            this.PutInteger4(nameIdx);
            this.PutInteger4(typeIdx);

            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {field.Name} {field.FieldType}");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="field"></param>
        /// <exception cref="FieldIsNotDeclaredException"></exception>
        /// <exception cref="InvalidOpCodeException"></exception>
        /// <remarks>
        /// Only <see cref="OpCodes.LDF"/>.
        /// </remarks>
        public virtual void Emit(OpCode opcode, FieldName field)
        {
            throw new NotImplementedException();

            if (opcode.Value != (int)OpCodes.LDF.Value)
                throw new InvalidOpCodeException($"Opcode '{opcode.Name}' is not allowed.");

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
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {field.Name}.{token:X8}");
        }
        /// <summary>
        /// Emit branch instruction with label.
        /// </summary>
        public virtual void Emit(OpCode opcode, Label label)
        {
            this.EnsureCapacity<OpCode>(sizeof(int));
            this.InternalEmit(opcode);
            this.PutInteger4(label.Value);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} label(0x{label.Value:X})");
        }
        public virtual void Emit(OpCode opcode, ManaType type)
            => Emit(opcode, type.FullName);
        public virtual void Emit(OpCode opcode, ManaClass type)
            => Emit(opcode, type.FullName);

        public virtual void Emit(OpCode opcode, QualityTypeName type)
        {
            this.EnsureCapacity<OpCode>(sizeof(int));
            this.InternalEmit(opcode);
            this.PutTypeName(type);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */.{opcode.Name} [{type}] ");
        }
        /// <summary>
        /// Emit LOC_INIT.
        /// </summary>
        /// <exception cref="InvalidOpCodeException"></exception>
        /// <remarks>
        /// Only <see cref="OpCodes.LOC_INIT"/>.
        /// <br/>
        /// <see cref="OpCodes.LOC_INIT_X"/> will be automatic generated.
        /// </remarks>
        private void WriteLocals(BinaryWriter bin)
        {
            if (LocalsSize == 0)
                return;
            var size = LocalsBuilder.Count();

            bin.Write((ushort)OpCodes.LOC_INIT.Value);
            bin.Write((int)size);
            foreach (var t in LocalsBuilder)
            {
                bin.Write((ushort)OpCodes.LOC_INIT.Value);
                this.PutTypeNameInto(t, bin);
            }
        }

        public LocalsBuilder LocalsBuilder { get; } = new();
        /// <summary>
        /// Ensure local slot
        /// </summary>
        /// <returns>
        /// Index of local variable slot
        /// </returns>
        public virtual int EnsureLocal(string key, ManaClass clazz)
        {
            LocalsBuilder.Mark(LocalsSize, key);
            LocalsBuilder.Push(clazz);
            return LocalsSize++;
        }
        /// <summary>
        /// Emit call.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOpCodeException"></exception>
        /// <remarks>
        /// Only <see cref="OpCodes.CALL"/>.
        /// </remarks>
        public virtual void Emit(OpCode opcode, ManaMethod method)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (opcode != OpCodes.CALL)
                throw new InvalidOpCodeException($"Opcode '{opcode.Name}' is not allowed.");
            var (tokenIdx, ownerIdx) = this._methodBuilder.classBuilder.moduleBuilder.GetMethodToken(method);
            this.EnsureCapacity<OpCode>(
                sizeof(byte) /*CallContext */ +
                sizeof(int) /* tokenIdx */ +
                sizeof(int) /* ownerIdx */);
            this.InternalEmit(opcode);


            /* todo: remove directing call flag */
            if (method.Owner.FullName == this._methodBuilder.Owner.FullName)
                this.PutByte((byte)CallContext.THIS_CALL);
            else if (method.IsExtern)
                this.PutByte((byte)CallContext.NATIVE_CALL);
            else if (method.IsStatic)
                this.PutByte((byte)CallContext.STATIC_CALL);
            else
                this.PutByte((byte)CallContext.BACKWARD_CALL);

            this.PutInteger4(tokenIdx);
            this.PutTypeName(ownerIdx);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {method}");
        }

        internal void WriteDebugMetadata(string str)
        {
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ {str}");
        }

        internal enum FieldDirection
        {
            Arg,
            Local,
            Member
        }

        private int[] _labels;
        private int _labels_count;
        /// <summary>
        /// Define label for future use. 
        /// </summary>
        /// <returns></returns>
        public virtual Label DefineLabel()
        {
            _labels ??= new int[4];
            if (_labels_count >= _labels.Length)
                RepackArray(_labels);
            _labels[_labels_count] = -1;
            return new Label(_labels_count++);
        }
        /// <summary>
        /// Define multiple labels for future use.
        /// </summary>
        public virtual Label[] DefineLabel(uint size) =>
            Enumerable.Range(0, (int)size).Select(_ => DefineLabel()).ToArray();

        /// <summary>
        /// Use label in current position.
        /// </summary>
        /// <exception cref="InvalidLabelException"></exception>
        /// <exception cref="UndefinedLabelException"></exception>
        public virtual void UseLabel(Label loc)
        {
            if (_labels is null || loc.Value < 0 || loc.Value >= _labels.Length)
                throw new InvalidLabelException();
            if (_labels[loc.Value] != -1)
                throw new UndefinedLabelException();
            _labels[loc.Value] = _position;
            _debugBuilder.AppendLine($"/* defined-label id: 0x{loc.Value:X}, offset: 0x{_position:X} */");
        }
        /// <summary>
        /// Get labels positions.
        /// </summary>
        public int[] GetLabels() => _labels;


        private (ulong, FieldDirection) FindFieldToken(FieldName field)
        {
            var token = (ulong?)0;
            if ((token = this._methodBuilder.FindArgumentField(field)) != null)
                return (token.Value, FieldDirection.Arg);
            if ((token = this._methodBuilder.FindLocalField(field)) != null)
                return (token.Value, FieldDirection.Local);
            if ((token = this._methodBuilder.classBuilder.FindMemberField(field)) != null)
                return (token.Value, FieldDirection.Member);
            throw new FieldIsNotDeclaredException(field);
        }

        internal byte[] BakeByteArray()
        {
            if (_position == 0)
                return new byte[0];
            using var mem = new MemoryStream();
            using var bin = new BinaryWriter(mem);

            Array.Resize(ref _ilBody, _position);
            WriteLocals(bin);
            bin.Write(_ilBody);
            bin.Write((ushort)0xFFFF); // end frame
            bin.Write(_labels_count);
            if (_labels_count == 0)
                return mem.ToArray();
            foreach (var i in _labels)
                bin.Write(i);
            return mem.ToArray();
        }

        internal string BakeDebugString()
            => _position == 0 ? "" : $"{LocalsBuilder}\n{_debugBuilder}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutInteger4(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), value);
            _position += sizeof(int);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutByte(byte value)
        {
            _ilBody[_position] = value;
            _position += sizeof(byte);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutUInteger8(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_ilBody.AsSpan(_position), value);
            _position += sizeof(ulong);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutInteger8(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_position), value);
            _position += sizeof(long);
        }

        internal void InternalEmit(OpCode opcode)
        {
            var num = opcode.Value;
            BinaryPrimitives.WriteUInt16LittleEndian(_ilBody.AsSpan(_position), num);
            _position += sizeof(ushort);
            _opcodes.Add(num);
            //this.UpdateStackSize(opcode, opcode.StackChange());
        }
        internal void EnsureCapacity<_>(params int[] sizes) where _ : struct
        {
            var sum = sizes.Sum() + sizeof(ushort);
            EnsureCapacity(sum);
        }
        internal void EnsureCapacity(int size)
        {
            if (_position + size < _ilBody.Length)
                return;
            IncreaseCapacity(size);
        }

        private void IncreaseCapacity(int size)
        {
            var newsize = Math.Max(_ilBody.Length * 2, _position + size + 2);
            if (newsize % 2 != 0)
                newsize++;
            var numArray = new byte[newsize];
            Array.Copy(_ilBody, numArray, _ilBody.Length);
            _ilBody = numArray;
        }



        internal static T[] RepackArray<T>(T[] arr) => RepackArray<T>(arr, arr.Length * 2);

        internal static T[] RepackArray<T>(T[] arr, int newSize)
        {
            var objArray = new T[newSize];
            Array.Copy(arr, objArray, arr.Length);
            return objArray;
        }

        public Dictionary<string, object> Metadata { get; } = new();

        public T ConsumeFromMetadata<T>(string key) where T : class => Metadata[key] as T;
        public void StoreIntoMetadata(string key, object o) => Metadata.Add(key, o);
    }
}
