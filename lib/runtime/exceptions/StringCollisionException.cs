namespace insomnia.exceptions
{
    using System;

    public class StringCollisionException : Exception
    {
        public StringCollisionException(string first, string second) : base(
            $"Detected collisions of string constant. '{first}' and '{second}'.\n " +
            "Please report this issue into https://github.com/0xF6/wave_lang/issues.") { }
    }
}