namespace mana.ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using global::ishtar;
    using mana.extensions;
    using runtime;

    internal static unsafe class ILReader
    {
        public static List<int> DeconstructLabels(byte[] arr, int offset)
        {
            if (arr.Length == 0)
                return new();

            using var mem = new MemoryStream(arr);
            using var bin = new BinaryReader(mem);

            mem.Seek(offset, SeekOrigin.Begin);

            var labels_size = bin.ReadInt32();

            if (labels_size == 0)
                return new List<int>();
            var result = new List<int>();
            foreach (var i in ..labels_size)
                result.Add(bin.ReadInt32());
            return result;
        }
        public static (List<uint> opcodes, Dictionary<int, (int pos, OpCodeValue opcode)> map) Deconstruct(byte[] arr, ManaMethod method)
        {
            var i = 0;
            return Deconstruct(arr, &i, method);
        }
        public static (List<uint> opcodes, Dictionary<int, (int pos, OpCodeValue opcode)> map) Deconstruct(byte[] arr, int* offset, ManaMethod method)
        {
            using var mem = new MemoryStream(arr);
            using var bin = new BinaryReader(mem);

            var list = new List<uint>();
            var d = new Dictionary<int, (int pos, OpCodeValue opcode)>();

            string PreviousValue(int index)
            {
                var i = list[^index];
                if (OpCodes.all.ContainsKey((OpCodeValue)i))
                    return $"{OpCodes.all[(OpCodeValue)i].Name}";
                return $"0x{i:X}:{i}";
            }

            while (mem.Position < mem.Length)
            {
                var opcode = (OpCodeValue) bin.ReadUInt16();

                if ((ushort)opcode == 0xFFFF)
                {
                    *offset = (int)mem.Position;
                    return (list, d);
                }

                list.Add((uint)opcode);
                if (!OpCodes.all.ContainsKey(opcode))
                    throw new InvalidOperationException(
                    $"OpCode '{opcode}' is not found in metadata.\n" +
                    $"re-run 'gen.csx' for fix this error.\n" +
                    $"Previous values: '{PreviousValue(1)}, {PreviousValue(2)}, {PreviousValue(3)}'.\n" +
                    $"Method: '{method.Name}' in '{method.Owner.Name}'.");
                var value = OpCodes.all[opcode];

                d.Add((int)mem.Position - sizeof(ushort), (list.Count, opcode));
                switch (value.Size)
                {
                    // call
                    case var _ when value.Value == (ushort)OpCodeValue.CALL:
                        // 1+4+4
                        list.Add((uint)bin.ReadByte());
                        list.Add((uint)bin.ReadInt32());
                        list.Add((uint)bin.ReadInt32());
                        break;
                    case var _ when value.Value == (ushort)OpCodeValue.LOC_INIT:
                        list.Add((uint)bin.ReadInt32()); // size
                        break;
                    case var _ when value.Value == (ushort)OpCodeValue.LOC_INIT_X:
                        list.Add((uint)bin.ReadInt32()); // type_index
                        break;
                    case var _ when value.Value == (ushort)OpCodeValue.LDC_I8_S:
                    case var _ when value.Value == (ushort)OpCodeValue.LDC_F8:
                        list.Add((uint)bin.ReadInt32()); // slot 1
                        list.Add((uint)bin.ReadInt32()); // slot 2
                        break;
                    case var _ when value.Value == (ushort)OpCodeValue.LDC_F16:
                        var size = bin.ReadInt32();
                        list.Add((uint)size); // size
                        foreach (var _ in ..size)
                            list.Add((uint)bin.ReadInt32()); // slot 1
                        break;
                    case var _ when value is
                    { Value: (ushort)OpCodeValue.LDF } or
                    { Value: (ushort)OpCodeValue.STF } or
                    { Value: (ushort)OpCodeValue.STSF } or
                    { Value: (ushort)OpCodeValue.LDSF }:
                        list.Add((uint)bin.ReadInt32()); // name_index
                        list.Add((uint)bin.ReadInt32()); // type_index
                        break;
                    case 0:
                        continue;
                    case sizeof(byte):
                        list.Add((uint)bin.ReadByte());
                        break;
                    case sizeof(short):
                        list.Add((uint)bin.ReadInt16());
                        break;
                    case sizeof(int):
                        list.Add((uint)bin.ReadInt32());
                        break;
                    case sizeof(long):
                        list.Add((uint)bin.ReadInt32());
                        list.Add((uint)bin.ReadInt32());
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"OpCode '{opcode}' has invalid size [{value.Size}].\n" +
                            $"Check 'opcodes.def' and re-run 'gen.csx' for fix this error.");
                }
            }
            return (list, d);
        }
    }
}
