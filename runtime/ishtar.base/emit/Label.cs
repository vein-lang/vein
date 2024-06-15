namespace ishtar.emit
{
    using System;

    public readonly record struct Label(int val, string name)
    {
        internal int Value { get; } = val;
        internal string Name { get; } = name;
    }
}
