namespace wave
{
    using System.Collections.Generic;
    using emit.opcodes;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var frags = new List<Fragment>();

            frags.AddRange(new Fragment[]
            {
                new F_LABEL("add"),
                new F_MV("sra", "isa"),
                new F_IMUL("sra", "isa"),
                new F_DROP()
            });
        }
    }
}