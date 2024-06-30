namespace ishtar
{
    using System.Collections.Generic;
    using ishtar;

    public partial class VirtualMachine
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

            private static readonly Dictionary<SysFlag, string> _cache = new(64);


            #region Defined Configs

            public bool UseDebugAllocator => Has("has_debug_allocator");
            public bool DisabledFinalization => Has("has_disabled_finalization");
            public bool CallOpCodeSkipValidateArgs => Has("skip-validate-args"); //--sys::ishtar::skip-validate-args=1
            public bool DisableValidationInvocationArgs => Has("has_disabled_validation_inv_args");


            #endregion

        }



    }


}
