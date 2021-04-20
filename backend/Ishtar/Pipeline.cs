namespace ishtar
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;
    using System.IO;
    using wave.runtime;
    using wave.fs;

    public class Pipeline : Command
    {
        public async Task<int> GenerateRuntimeAsync()
        {
            //Console.WriteLine($"{":gear:".Emoji()} Initialized regeneration runtime libraries...");

            
            //var stllib = new WaveModuleBuilder("stl");
            //Console.WriteLine($"{":smoking:".Emoji()} Generate stl.lib...");
            
            //BuiltinGen.GenerateConsole(stllib);
            
            //var asm = new IshtarAssembly(stllib);
            //IshtarAssembly.WriteTo(asm, 
            //    new DirectoryInfo(@"C:\Program Files (x86)\WaveLang\sdk\0.1-preview\runtimes\any"));
            //await File.WriteAllTextAsync(@"C:\Program Files (x86)\WaveLang\sdk\0.1-preview\runtimes\any\stl.wll.il",
            //    stllib.BakeDebugString());

            return await Success();
        }

        public async Task<int> StartAsync(DirectoryInfo sources)
        {
            return default;
        }



        public static Pipeline Create() => new();


    }
    
    
    public abstract class Command
    {
        protected static Task<int> Success() => Task.FromResult(0);
        protected static Task<int> Fail() => Task.FromResult(1);
        protected static Task<int> Fail(int status) => Task.FromResult(status);
        protected static Task<int> Fail(string text)
        {
            //Console.WriteLine($"{":x:".Emoji()} {text.Color(Color.Red)}");
            return Fail();
        }
        protected static Task<int> Success(string text)
        {
            //Console.WriteLine($"{":heavy_check_mark:".Emoji()} {text.Color(Color.GreenYellow)}");
            return Success();
        }
    }
}