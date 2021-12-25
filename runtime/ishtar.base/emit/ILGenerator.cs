namespace ishtar.emit
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using extensions;
    using global::ishtar;
    using vein.extensions;
    using vein.runtime;
    using static global::ishtar.OpCodeValue;

    public class ILGenerator
    {
        private byte[] _ilBody;
        private int _position;
        internal List<ushort> _opcodes = new ();
        internal List<OpCode> _debug_list = new ();
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

        public virtual ILGenerator Emit(OpCode opcode)
        {
            EnsureCapacity<OpCode>();
            InternalEmit(opcode);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name}");
            return this;
        }
        public virtual ILGenerator Emit(OpCode opcode, byte arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(byte));
            InternalEmit(opcode);
            _ilBody[_position++] = arg;
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.byte");
            return this;
        }
        public ILGenerator Emit(OpCode opcode, sbyte arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(sbyte));
            InternalEmit(opcode);
            _ilBody[_position++] = (byte)arg;
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.sbyte");
            return this;
        }
        public virtual ILGenerator Emit(OpCode opcode, short arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(short));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt16LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(short);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.short");
            return this;
        }

        public virtual ILGenerator Emit(OpCode opcode, int arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(int));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(int);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.int");
            return this;
        }
        public virtual ILGenerator Emit(OpCode opcode, long arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(long));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(long);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.long");
            return this;
        }

        public virtual ILGenerator Emit(OpCode opcode, ulong arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(ulong));
            InternalEmit(opcode);
            BinaryPrimitives.WriteUInt64LittleEndian(_ilBody.AsSpan(_position), arg);
            _position += sizeof(ulong);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} 0x{arg:X8}.ulong");
            return this;
        }

        public virtual ILGenerator Emit(OpCode opcode, float arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(float));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(_ilBody.AsSpan(_position), BitConverter.SingleToInt32Bits(arg));
            _position += sizeof(float);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {arg}.float");
            return this;
        }

        public virtual ILGenerator Emit(OpCode opcode, double arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            EnsureCapacity<OpCode>(sizeof(double));
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(_ilBody.AsSpan(_position), BitConverter.DoubleToInt64Bits(arg));
            _position += sizeof(double);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {arg}.double");
            return this;
        }

        public virtual ILGenerator Emit(OpCode opcode, decimal arg)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

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
            return this;
        }
        /// <remarks>
        /// <see cref="string"/> will be interned.
        /// </remarks>
        public virtual ILGenerator Emit(OpCode opcode, string str)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            var token = _methodBuilder
                .classBuilder
                .moduleBuilder
                .InternString(str);
            this.EnsureCapacity<OpCode>(sizeof(int));
            InternalEmit(opcode);
            PutInteger4(token);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} .0x{token:X8}");
            return this;
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
        public virtual ILGenerator Emit(OpCode opcode, VeinField field)
        {
            if (new[] { OpCodes.LDF, OpCodes.STF, OpCodes.STSF, OpCodes.LDSF }.All(x => x != opcode))
                throw new InvalidOpCodeException($"Opcode '{opcode.Name}' is not allowed.");

            if (this._methodBuilder.IsConstructor && opcode == OpCodes.STF) // shit
                this.FieldMarkAsInited(field);

            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            this.EnsureCapacity<OpCode>(sizeof(int) * 2);
            this.InternalEmit(opcode);

            var nameIdx = this._methodBuilder.moduleBuilder.InternFieldName(field.FullName);
            var typeIdx = this._methodBuilder.moduleBuilder.InternTypeName(field.Owner.FullName);

            this.PutInteger4(nameIdx);
            this.PutInteger4(typeIdx);

            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {field.Name} {field.FieldType}");
            return this;
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
        public virtual ILGenerator Emit(OpCode opcode, FieldName field) => throw new NotImplementedException();

        /// <summary>
        /// Emit branch instruction with label.
        /// </summary>
        public virtual ILGenerator Emit(OpCode opcode, Label label)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            this.EnsureCapacity<OpCode>(sizeof(int));
            this.InternalEmit(opcode);
            this.PutInteger4(label.Value);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} label(0x{label.Value:X})");
            return this;
        }
        public virtual ILGenerator Emit(OpCode opcode, VeinClass type)
            => Emit(opcode, type.FullName);

        public virtual ILGenerator Emit(OpCode opcode, QualityTypeName type)
        {
            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            this.EnsureCapacity<OpCode>(sizeof(int));
            this.InternalEmit(opcode);
            this.PutTypeName(type);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} [{type}] ");
            return this;
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

        private uint GetLocalsILOffset()
        {
            if (LocalsSize == 0)
                return 0;

            var size = (uint)LocalsBuilder.Count();
            var offset = 2u /* opcode size */ + (uint)OpCodes.LOC_INIT.Size /* body of opcode size */;

            offset += size * offset; /* multiple to size locals */
            return offset;
        }

        public LocalsBuilder LocalsBuilder { get; } = new();
        /// <summary>
        /// Ensure local slot
        /// </summary>
        /// <returns>
        /// Index of local variable slot
        /// </returns>
        public virtual int EnsureLocal(string key, VeinClass clazz)
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
        public virtual ILGenerator Emit(OpCode opcode, VeinMethod method)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));
            if (opcode != OpCodes.CALL)
                throw new InvalidOpCodeException($"Opcode '{opcode.Name}' is not allowed.");

            using var _ = ILCapacityValidator.Begin(ref _position, opcode);

            var (tokenIdx, ownerIdx) = this._methodBuilder.classBuilder.moduleBuilder.GetMethodToken(method);
            this.EnsureCapacity<OpCode>(
                sizeof(byte) /*CallContext */ +
                sizeof(int) /* tokenIdx */ +
                sizeof(int) /* ownerIdx */);
            this.InternalEmit(opcode);

            this.PutInteger4(tokenIdx);
            this.PutTypeName(ownerIdx);
            _debugBuilder.AppendLine($"/* ::{_position:0000} */ .{opcode.Name} {method}");
            return this;
        }
        
        #region Exceptions

        private ExceptionBlockInfo[] exceptionBlocks;
        private ExceptionBlockInfo[] exceptionStack;
        private uint exceptionIndex;
        private uint exceptionStackIndex;

        public ExceptionBlockInfo[] GetExceptions()
        {
            if (exceptionBlocks is null)
                return new ExceptionBlockInfo[0];
            return exceptionBlocks.Where(x => x is not null).ToArray() ?? new ExceptionBlockInfo[0];
        }

        public VeinClass[] GetEffectedExceptions()
        {
            if (exceptionIndex == 0)
                return new VeinClass[0];
            return new HashSet<VeinClass>(exceptionBlocks.Where(x => x is not null)
                .SelectMany(x => x.CatchClass).Where(x => x is not null)).ToArray();
        }

        public Label BeginTryBlock()
        {
            exceptionBlocks ??= new ExceptionBlockInfo[8];
            exceptionStack ??= new ExceptionBlockInfo[8];

            if (exceptionIndex >= exceptionBlocks.Length)
                exceptionBlocks = IncreaseCapacity(exceptionBlocks);
            if (exceptionStackIndex >= exceptionStack.Length)
                exceptionStack = IncreaseCapacity(exceptionStack);

            Emit(OpCodes.SEH_ENTER, (int)exceptionIndex);

            var label = DefineLabel();
            var info = new ExceptionBlockInfo(ILOffset, label);

            exceptionBlocks[exceptionIndex++] = info;
            exceptionStack[exceptionStackIndex++] = info;

            WriteDebugMetadata($"/* begin protection zone @0x{info.StartAddr:X} */");
            
            return label;
        }
        
        public void EndExceptionBlock()
        {
            if (exceptionStackIndex == 0)
                throw new InvalidOperationException($"Exception stack has been empty!");

            var info = exceptionStack![exceptionStackIndex - 1];
            exceptionStack![--exceptionStackIndex] = null;

            var EndLabel = info.EndLabel;

            if (info.State is ExceptionBlockState.TRY)
                throw new InvalidOperationException($"Exception state is incorrect!");

            if (info.State is ExceptionBlockState.CATCH)
                Emit(OpCodes.SEH_LEAVE);
            if (info.State is ExceptionBlockState.FINALLY)
                Emit(OpCodes.SEH_FINALLY);

            Label label = _labels![EndLabel.Value] != -1
                ? info.FinallyLabel
                : EndLabel;
            UseLabel(label);

            info.Done(ILOffset);
            WriteDebugMetadata($"/* end protection zone @0x{info.StartAddr:X} */");
        }

        public int BeginCatchBlock(VeinClass? type)
        {
            if (exceptionStackIndex == 0)
                throw new InvalidOperationException($"Exception stack has been empty!");

            var info = exceptionStack![exceptionStackIndex - 1];

            Emit(OpCodes.SEH_LEAVE_S, info.EndLabel);

            /* temporary shit */
            Emit(OpCodes.NOP);
            var label = DefineLabel();
            UseLabel(label);
            /*                */

            info.MarkCatchAddr(label.Value, type);
            WriteDebugMetadata($"/* catch block $'{(type is null ? "any" : type.Name)}' @{info.StartAddr} */");
            return info.CurrentCatch;
        }
        
        public virtual void BeginFinallyBlock()
        {
            if (exceptionStackIndex == 0)
                throw new InvalidOperationException($"Exception stack has been empty!");
            var info = exceptionStack![exceptionStackIndex - 1];
            var state = info.State;
            Label endLabel = info.EndLabel;
            if (state != ExceptionBlockState.TRY)
                Emit(OpCodes.SEH_LEAVE_S, endLabel);
            UseLabel(endLabel);

            Label finallyEndLabel = DefineLabel();
            info.SetFinallyEndLabel(finallyEndLabel);
            info.MarkFinallyAddr(ILOffset);
            WriteDebugMetadata($"/* finally block @0x{info.StartAddr:X} */");
        }

        #endregion
        

        public ILGenerator WriteDebugMetadata(string str, int pos = 0)
        {
            if (pos == 0) pos = _position;
            _debugBuilder.AppendLine($"/* ::{pos:0000} */ {str}");
            return this;
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
                _labels = IncreaseCapacity(_labels);
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
            var loc_offset = GetLocalsILOffset();
            foreach (var i in .._labels_count)
                bin.Write((int)(_labels[i] + loc_offset));
            bin.Write((ushort)0xF00F);
            var exceptions = GetExceptions();
            bin.Write(exceptions.Length);
            foreach (var excpt in exceptions)
            {
                bin.Write(excpt.StartAddr);
                bin.Write(excpt.EndAddr);
                bin.WriteArray(excpt.FilterAddr, (x, y) => y.Write(x));
                bin.WriteArray(excpt.CatchAddr, (x, y) => y.Write(x));
                bin.WriteArray(excpt.CatchClass.Where(x => x is not null).ToArray(),
                    (x, y) => y.WriteTypeName(x.FullName, this._methodBuilder.moduleBuilder));
                bin.WriteArray(excpt.Types, (x, y) => y.Write((byte)x));
            }
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
            _debug_list.Add(opcode);
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

        [Pure]
        internal static T[] IncreaseCapacity<T>(T[] incoming)
            => IncreaseCapacity(incoming, incoming.Length * 2);
        [Pure]
        internal static T[] IncreaseCapacity<T>(T[] incoming, int requiredSize)
        {
            Debug.Assert(incoming != null);

            T[] temp = new T[requiredSize];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        public Dictionary<string, object> Metadata { get; } = new();

        public T ConsumeFromMetadata<T>(string key) where T : class => Metadata[key] as T;
        public void StoreIntoMetadata(string key, object o) => Metadata.Add(key, o);
        public bool HasMetadata(string key) => Metadata.ContainsKey(key);
    }


    public static class ILGeneratorExtensions
    {
        private static string castFieldToMetadataKey(VeinField field) =>
            $"key_{field.Owner.FullName}$${field.Name}";

        public static bool FieldHasAlreadyInited(this ILGenerator generator, VeinField field)
            => generator.HasMetadata(castFieldToMetadataKey(field));

        public static void FieldMarkAsInited(this ILGenerator generator, VeinField field)
            => generator.StoreIntoMetadata(castFieldToMetadataKey(field), true);
    }
}
