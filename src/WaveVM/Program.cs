namespace wave
{
    using System.Collections.Generic;
    using runtime.emit.@unsafe;

    public class NOP : Fragment
    {
        public NOP() : base(0x0)
        {
        }

        protected override string ToTemplateString()
            => ":nop";
    }

    public class F_MV : Fragment, IArg<byte>
    {
        protected readonly byte _register;
        protected readonly byte _slot;

        public F_MV(string register, string slot)
            : this(Storage.GetRegisterByLabel(register), Storage.GetSlotByLabel(slot))
        { }

        public F_MV(byte register, byte slot) : base(0xAD)
        {
            _register = register;
            _slot = slot;
        }

        public byte Get()
            => d8u.Null.Construct(_register, _slot);

        protected override string ToTemplateString()
            => $":mv {Storage.GetRegisterByIndex(_register)}, {Storage.GetSlotByIndex(_slot)}";
    }

    public class F_IMUL : F_MV
    {
        public F_IMUL(string register, string slot) : base(register, slot)
        {
        }

        public F_IMUL(byte register, byte slot) : base(register, slot)
        {
        }

        protected override string ToTemplateString()
            => $":imul {Storage.GetRegisterByIndex(_register)}, {Storage.GetSlotByIndex(_slot)}";
    }

    public class F_LABEL : Fragment, IInterningProvider
    {
        private readonly string _labelId;

        public F_LABEL(string labelID) : base(0x2)
            => _labelId = labelID;

        protected override string ToTemplateString()
            => $"+{_labelId}:";

        public string[] GetForInterning()
        {
            return new[] { _labelId };
        }
    }

    public interface IInterningProvider
    {
        string[] GetForInterning();
    }

    public class F_DROP : Fragment
    {
        public F_DROP() : base(0x1)
        {
        }

        protected override string ToTemplateString()
            => ":drop";
    }

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