namespace insomnia
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Spectre.Console;

    public static class ColorShim
    {
        private static bool is_applied;
        public static void Apply()
        {
            if (is_applied)
                return;
            var asm = typeof(AnsiConsole).Assembly;

            var table = asm.GetType("Spectre.Console.ColorTable");

            var _numberLookup = table.GetField("_numberLookup", BindingFlags.Static | BindingFlags.NonPublic);
            var _nameLookup = table.GetField("_nameLookup", BindingFlags.Static | BindingFlags.NonPublic);

            var numberLookup = (_numberLookup.GetValue(null) as Dictionary<string, int>);
            var nameLookup = (_nameLookup.GetValue(null) as Dictionary<int, string>);

            //var gray = numberLookup["grey"];
            //numberLookup.Add("gray", gray);

            var orange = numberLookup["orange3"];
            numberLookup.Add("orange", orange);

            is_applied = true;
        }
    }
}
