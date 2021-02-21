namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class ILReader
    {
        public static List<uint> Deconstruct(byte[] arr)
        {
            using var mem = new MemoryStream(arr);
            using var bin = new BinaryReader(mem);

            var list = new List<uint>();

            while (mem.Position < mem.Length)
            {
                var opcode = (OpCodeValue) bin.ReadUInt16();
                list.Add((uint)opcode);
                if (!OpCodes.all.ContainsKey(opcode))
                    throw new InvalidOperationException(
                    $"OpCode '{opcode}' is not found in metadata.\n" +
                    $"re-run 'gen.csx' for fix this error.");
                var value = OpCodes.all[opcode];
                
                switch (value.Size)
                {
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
                    case sizeof(decimal):
                        list.Add((uint)bin.ReadInt32());
                        list.Add((uint)bin.ReadInt32());
                        list.Add((uint)bin.ReadInt32());
                        list.Add((uint)bin.ReadInt32());
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"OpCode '{opcode}' has invalid size [{value.Size}].\n"+
                            $"Check 'opcodes.def' and re-run 'gen.csx' for fix this error.");
                }
            }

            return list;
        }
    }
}