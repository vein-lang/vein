using System;
using System.Threading.Tasks;

namespace wave
{
    using Pastel;
    using static System.Console;
    using Color = System.Drawing.Color;
    
    internal class _term
    {
        private static readonly object Guarder = new object();


        public static Task<int> Success() => Task.FromResult(0);
        public static Task<int> Fail() => Task.FromResult(1);
        public static Task<int> Fail(int status) => Task.FromResult(status);
        public static Task<int> Fail(string text)
        {
            Console.WriteLine($"{":x:".Emoji()} {text.Color(Color.Red)}");
            return Fail();
        }
        public static void Trace(string message)
        {
            lock (Guarder)
            {
                WriteLine($"trace: {message}".Pastel(Color.Gray));
            }
        }
        public static void Success(string message)
        {
            lock (Guarder)
            {
                Write("[");
                Write($"SUCCESS".Pastel(Color.YellowGreen));
                Write("]: ");
                WriteLine($" {message}");
            }
        }
        public static void Warn(string message)
        {
            lock (Guarder)
            {
                Write("[");
                Write($"WARN".Pastel(Color.Orange));
                Write("]: ");
                WriteLine($" {message}");
            }
        }
        public static void Error(string message)
        {
            lock (Guarder)
            {
                Write("[");
                Write($"ERROR".Pastel(Color.Red));
                Write("]: ");
                WriteLine($" {message}");
            }
        }
    }
}