namespace wave.emit
{
    using System.Collections;
    using System.Collections.Generic;
    /// <summary>
    /// Locals builder.
    /// </summary>
    public class LocalsBuilder : IEnumerable<QualityTypeName>
    {
        private readonly IList<QualityTypeName> types = new List<QualityTypeName>();
        public void Push(QualityTypeName type) 
            => types.Add(type);
        public void Push(WaveType type) 
            => types.Add(type.FullName);
        public void Push(WaveTypeCode type)
            => types.Add(type.AsType().FullName);


        #region Implementation of IEnumerable

        public IEnumerator<QualityTypeName> GetEnumerator() 
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
        public static implicit operator LocalsBuilder(QualityTypeName[] arr)
        {
            var l = new LocalsBuilder();
            foreach (var type in arr) 
                l.Push(type);
            return l;
        }
    }
}