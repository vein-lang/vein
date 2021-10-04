namespace vein
{
    using System.Collections.Generic;
    using common;
    using Serilog;

    internal class Journal
    {
        private static readonly Dictionary<string, ILogger> loggers = new ();


        public static ILogger Get(string name)
        {
            if (loggers.ContainsKey(name))
                return loggers[name];
            var result = JournalFactory.RegisterGroup(name);
            loggers.Add(name, result);
            return result;
        }
    }
}
