namespace ishtar;

internal class IshtarTrace
{
    private static bool useConsole;
    private static bool useFile;

    public IshtarTrace()
    {
        useConsole = Environment.GetCommandLineArgs().Contains("--sys::log::use-console=1");
        useFile = Environment.GetCommandLineArgs().Contains("--sys::log::use-file=1"); // TODO
    }

    public void println(string s)
    {
        //if (useConsole)
        {
            Console.WriteLine(s);
        }
    }
}
