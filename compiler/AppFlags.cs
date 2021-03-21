namespace wave
{
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;
    using Sprache;

    public static class AppFlags
    {
        private static Dictionary<string, string> flags = new();
        // filter only extra flag
        public static void RegisterArgs(string[] args) => args
            .Where(x => x.StartsWith("--EF"))
            .Select(ParserExtraFlag.unit.End().Parse)
            .Where(x => !flags.ContainsKey(x.Key))
            .ForEach(x => flags.Add(x.Key, x.Value));

        public static void Set(string key, bool val)
            => flags.Add(key, val.ToString().ToLowerInvariant());
        // TODO
        public static bool HasFlag(string key)
        {
            if (flags.ContainsKey(key) && flags[key] is "false" or "true")
                return flags[key] == "true";
            return false;
        }

        private class ParserExtraFlag
        {
            public static Parser<KeyValuePair<string, string>> unit =>
                from ef in Parse.String("--EF")
                from s in Parse.Char(':')
                from data in (
                    from bl in Parse.Char('+').Or(Parse.Char('-'))
                    from key in Parse.Letter.AtLeastOnce().Text()
                    select new KeyValuePair<string, string>(key, bl == '+' ? "true" : "false")
                ).Or(
                    from key in Parse.Letter.AtLeastOnce().Text()
                    from s in Parse.Char('=')
                    from val in Parse.LetterOrDigit.AtLeastOnce().Text()
                    select new KeyValuePair<string, string>(key, val)
                )
                select data;
        }
    }
}