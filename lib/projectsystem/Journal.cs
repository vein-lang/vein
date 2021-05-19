namespace mana.project.@internal
{
    using mana.common;
    using Serilog;

    internal class Journal
    {
        private static ILogger _instance;

        public static ILogger logger
        {
            get
            {
                if (_instance is not null)
                    return _instance;
                return _instance = JournalFactory.RegisterGroup("project-system");
            }
        }
    }
}