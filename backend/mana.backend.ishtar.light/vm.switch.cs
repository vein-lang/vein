namespace ishtar
{
    using System.Collections.Generic;

    public static unsafe partial class VM
    {
        public static class Config
        {
            private static readonly IDictionary<string, bool> _switches = new Dictionary<string, bool>();

            public static void Set(string key, bool value) => _switches[key.ToLowerInvariant()] = value;

            public static bool Has(string key)
            {
                if (_switches.Count == 0) return false;

                key = key.ToLowerInvariant();
                if (!_switches.ContainsKey(key))
                    return false;
                return _switches[key];
            }
        }
    }
}
