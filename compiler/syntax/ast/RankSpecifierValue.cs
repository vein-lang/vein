namespace insomnia.syntax
{
    using Sprache;

    public class RankSpecifierValue : ExpressionSettingSyntax, IPositionAware<RankSpecifierValue>
    {
        public int Rank { get; set; }

        public RankSpecifierValue(int len)
        {
            this.Rank = len;
            this.ExpressionString = $"[{new string(',', len)}]";
        }
        public new RankSpecifierValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}