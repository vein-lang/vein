namespace ishtar
{
    using System.Collections.Generic;
    using vm;

    public partial class VM
    {
        public class VMConfig
        {
            private readonly IDictionary<string, bool> _switches = new Dictionary<string, bool>();

            public void Set(string key, bool value) => _switches[key.ToLowerInvariant()] = value;

            public bool Has(string key)
            {
                if (_switches.Count == 0) return false;

                key = key.ToLowerInvariant();
                if (!_switches.ContainsKey(key))
                    return false;
                return _switches[key];
            }

            public bool HasFlag(SysFlag flag)
            {
                var key = _cache.TryGetValue(flag, out string value) ?
                    value :
                    _cache[flag] = $"--sys::{flag.ToString().ToLowerInvariant().Replace("_", "-")}";


                var result = Environment.GetEnvironmentVariable(key) is not null;

                if (result) return true;

                return Has(key);
            }

            private static Dictionary<SysFlag, string> _cache = new Dictionary<SysFlag, string>(64);
        }



    }


}
