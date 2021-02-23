namespace wave.emit
{
    using System.Collections;
    using System.Collections.Generic;

    public class LocalsBuilder : IEnumerable<TypeName>
    {
        private readonly IList<TypeName> types = new List<TypeName>();
        public void Push(TypeName type) 
            => types.Add(type);

        public void Push(WaveType type) 
            => types.Add(type.FullName);
        public void Push(WaveTypeCode type)
            => types.Add(type.AsType().FullName);


        #region Implementation of IEnumerable

        public IEnumerator<TypeName> GetEnumerator() 
            => types.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        #endregion
        
        
        public static implicit operator LocalsBuilder(WaveType[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr) 
                l.Push(type);
            return l;
        }
        public static implicit operator LocalsBuilder(WaveTypeCode[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr) 
                l.Push(type);
            return l;
        }
        public static implicit operator LocalsBuilder(TypeName[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr) 
                l.Push(type);
            return l;
        }
    }
}