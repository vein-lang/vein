namespace mana.runtime
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Locals builder.
    /// </summary>
    public class LocalsBuilder : IEnumerable<QualityTypeName>
    {
        private readonly IList<QualityTypeName> types = new List<QualityTypeName>();
        private readonly Dictionary<int, string> locals_dictionary = new();
        public void Push(QualityTypeName type)
            => types.Add(type);
        public void Push(ManaType type)
            => types.Add(type.FullName);
        public void Push(ManaClass type)
            => types.Add(type.FullName);
        public void Push(ManaTypeCode type)
            => types.Add(type.AsClass().FullName);

        public void Mark(int index, string variable) => locals_dictionary[index] = variable;

        #region Implementation of IEnumerable

        public IEnumerator<QualityTypeName> GetEnumerator()
            => types.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion


        public static implicit operator LocalsBuilder(ManaType[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr)
                l.Push(type);
            return l;
        }
        public static implicit operator LocalsBuilder(ManaClass[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr)
                l.Push(type);
            return l;
        }
        public static implicit operator LocalsBuilder(ManaTypeCode[] arr)
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
            foreach (var (t, i) in this.Select((x, y) => (x, y)))
                str.AppendLine(locals_dictionary.ContainsKey(i)
                    ? $"\t[{i}]: {t.Name}, {{'{locals_dictionary[i]}'}}"
                    : $"\t[{i}]: {t.Name}");
            str.Append("};");
            return str.ToString();
        }
    }
}