namespace vein.runtime
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Locals builder.
    /// </summary>
    public class LocalsBuilder : IEnumerable<(QualityTypeName, VeinTypeArg)>
    {
        private readonly IList<(QualityTypeName, VeinTypeArg)> types = new List<(QualityTypeName, VeinTypeArg)>();
        private readonly Dictionary<int, string> locals_dictionary = new();
        public void Push(QualityTypeName type)
            => types.Add((type, null));
        public void Push(VeinClass type)
            => types.Add((type.FullName, null));

        public void Push(VeinTypeArg type)
            => types.Add((null, type));


        public void Mark(int index, string variable) => locals_dictionary[index] = variable;

        #region Implementation of IEnumerable

        public IEnumerator<(QualityTypeName, VeinTypeArg)> GetEnumerator()
            => types.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion


        public static implicit operator LocalsBuilder(VeinClass[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr)
                l.Push(type);
            return l;
        }

        public static implicit operator LocalsBuilder(QualityTypeName[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr)
                l.Push(type);
            return l;
        }


        public override string ToString()
        {
            if (types.Count == 0)
                return ".locals { }";
            var str = new StringBuilder();
            str.AppendLine(".locals { ");
            foreach (var ((t, g), i) in this.Select((x, y) => (x, y)))
            {
                if (locals_dictionary.TryGetValue(i, out string value))
                    str.AppendLine($"\t[{i}]: {(t == null ? g.Name : t.Name.name)}, {{'{value}'}}");
                else
                    str.AppendLine($"\t[{i}]: {(t == null ? g.Name : t.Name.name)}");
            }
            str.Append("};");
            return str.ToString();
        }
    }
}
