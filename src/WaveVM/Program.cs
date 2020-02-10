namespace wave
{
    using System.Collections.Generic;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var vm = new WaveVM();

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

    public class WaveVM
    {
        private Stack<byte> instructions { get; set; }

        public void Load(params byte[] ins)
            => instructions = new Stack<byte>(ins);

        public void Step()
        {
        }
    }
}