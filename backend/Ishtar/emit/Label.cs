namespace wave.ishtar.emit
{
    using System;

    public readonly struct Label : IEquatable<Label>
    {
        internal int Value { get; }

        public Label(int val) => this.Value = val;

        #region Equality members

        public bool Equals(Label other) => Value == other.Value;

        public override bool Equals(object obj) => obj is Label other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(Label left, Label right) => left.Equals(right);

        public static bool operator !=(Label left, Label right) => !left.Equals(right);

        #endregion
    }
}