namespace ishtar;

internal struct IshtarTrace()
{
    private bool useConsole = Environment.GetCommandLineArgs().Contains("--sys::log::use-console=1");
    private bool useFile = Environment.GetCommandLineArgs().Contains("--sys::log::use-file=1");

    public void println(string s)
    {
        //if (useConsole)
        {
            Console.WriteLine(s);
        }
    }
}
