namespace wave.common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using Serilog;
    
    public static class JournalFactory
    {
        private static readonly IDictionary<string, LoggerConfiguration> storage = 
            new ConcurrentDictionary<string, LoggerConfiguration>();
        private static readonly Func<string, LoggerConfiguration> _activator = defaultActivator;
        private static Action<LoggerConfiguration> _configurator;
        private static readonly object guarder = new object();
        public static ILogger RegisterGroup(string groupName)
        {
            lock (guarder)
            {
                if (storage.ContainsKey(groupName))
                    return storage[groupName].CreateLogger();
                var logger = _activator(groupName);
                storage.Add(groupName, logger);
                return logger.CreateLogger();
            }
        }


        public static void EnforceInitiator(Action<LoggerConfiguration> configurator) 
            => _configurator = configurator;


        private static LoggerConfiguration defaultActivator(string groupName)
        {
            new DirectoryInfo(Folder).Create();

            var result =  new LoggerConfiguration()
                .Enrich.WithProperty("group", groupName)
                .WriteTo.File($"{Folder}/{groupName}.log", rollingInterval: RollingInterval.Day);

            _configurator?.Invoke(result);
            return result;
        }

        private static string Folder =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Insomnia";
    }
}
