namespace wave.reflection
{
    using runtime;

    public class UnresolvedWaveClass : WaveClass
    {
        public UnresolvedWaveClass(QualityTypeName fullName) 
            => this.FullName = fullName;
    }
}