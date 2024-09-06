namespace vein
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Sprache;
    using static Spectre.Console.AnsiConsole;

    [AttributeUsage(AttributeTargets.Field)]
    public class ExperimentalAttribute : Attribute { }
    public enum ApplicationFlag
    {
        use_experimental_options,
        use_predef_array_type_initer,
        [Experimental]
        exp_simplify_optimize
    }

    public static class AppFlags
    {
        private static readonly Dictionary<string, string> flags = new();
        // filter only extra flag
        public static void RegisterArgs(ref string[] args)
        {
            var flagExperimental = args.Contains("--EF:+use_experimental_options");

            var values = GetAppFlagValues(flagExperimental ? FilterFlagValue.All : FilterFlagValue.Default);
            var expm = GetAppFlagValues(FilterFlagValue.OnlyExperimental);

            args
                .Where(x => x.StartsWith("--EF"))
                .Select(ParserExtraFlag.unit.End().Parse)
                .Where(x => !flags.ContainsKey(x.Key))
                .Where(x =>
                {
                    if (values.Contains(x.Key))
                        return true;
                    if (expm.Contains(x.Key))
                        MarkupLine($"[orange]WARN[/]: unsupported option '[red]{x.Key}[/]', put '[red]--EF:+use_experimental_options[/]' below all flags.");
                    else
                        MarkupLine($"[orange]WARN[/]: unknown option '[red]{x}[/]'.");
                    return false;
                })
                .ForEach(x => flags.Add(x.Key, x.Value));
            args = new List<string>(args.Where(x => !x.StartsWith("--EF:"))).ToArray();
        }

        private static string[] GetAppFlagValues(FilterFlagValue filter) => Enum.GetNames<ApplicationFlag>()
            .Where(x =>
            {
                ExperimentalAttribute Get() => typeof(ApplicationFlag).GetMember(x).Single()
                    .GetCustomAttribute<ExperimentalAttribute>();

                if (filter == FilterFlagValue.All)
                    return true;
                if (filter == FilterFlagValue.Default)
                    return Get() == null;
                return Get() != null;
            })
            .ToArray();

        private enum FilterFlagValue
        {
            Default,
            OnlyExperimental,
            All
        }

        public static void Set(ApplicationFlag key, bool val = true)
            => flags[$"{key}"] = val.ToString().ToLowerInvariant();

        private static void Set(string key, bool val)
            => flags.Add(key, val.ToString().ToLowerInvariant());
        // TODO
        private static bool HasFlag(string key)
        {
            if (flags.ContainsKey(key) && flags[key] is "false" or "true")
                return flags[key] == "true";
            return false;
        }

        public static bool HasFlag(ApplicationFlag key)
        {
            if (flags.ContainsKey($"{key}") && flags[$"{key}"] is "false" or "true")
                return flags[$"{key}"] == "true";
            return false;
        }

        private static class ParserExtraFlag
        {
            public static Parser<KeyValuePair<string, string>> unit =>
                from ef in Parse.String("--EF")
                from s in Parse.Char(':')
                from data in (
                    from bl in Parse.Char('+').Or(Parse.Char('-'))
                    from key in Parse.Letter.Or(Parse.Char('_')).AtLeastOnce().Text()
                    select new KeyValuePair<string, string>(key, bl == '+' ? "true" : "false")
                ).Or(
                    from key in Parse.Letter.Or(Parse.Char('_')).AtLeastOnce().Text()
                    from s in Parse.Char('=')
                    from val in Parse.LetterOrDigit.AtLeastOnce().Text()
                    select new KeyValuePair<string, string>(key, val)
                )
                select data;
        }
    }
}
