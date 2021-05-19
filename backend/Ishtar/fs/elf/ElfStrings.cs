namespace mana.fs.elf
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    public class ElfStrings
    {

        private readonly IDictionary<string, uint> _offsets = new Dictionary<string, uint>();
        private readonly List<byte> _data = new();

        public ElfStrings() => SaveString(string.Empty);

        public int BytesSize => _data.Count;

        public uint SaveString(string val)
        {
            if (_offsets.TryGetValue(val, out var offset)) 
                return offset;
            offset = (uint)_data.Count;
            var data = Encoding.ASCII.GetBytes(val);
            _data.AddRange(data);
            _data.Add(0);
            _offsets[val] = offset;
            return offset;
        }

        public byte[] ToArray() => _data.ToArray();
    }
}
