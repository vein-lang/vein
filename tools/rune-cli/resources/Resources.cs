namespace vein.resources;

public class Resources
{
    public static FileInfo Font => new FileInfo($"{AppDomain.CurrentDomain.BaseDirectory}/resources/isometric1.flf");
    public static FileInfo Licenses => new FileInfo($"{AppDomain.CurrentDomain.BaseDirectory}/resources/licenses.txt");
    public static FileInfo File(string name) => new FileInfo($"{AppDomain.CurrentDomain.BaseDirectory}/resources/{name}");
}
