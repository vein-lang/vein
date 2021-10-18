namespace vein.syntax;

using Sprache;

public partial class VeinSyntax
{
    /// <summary>
    /// (
    /// </summary>
    public static Parser<char> OPENING_PARENTHESIS = Parse.Char('(').Token();
    /// <summary>
    /// )
    /// </summary>
    public static Parser<char> CLOSING_PARENTHESIS = Parse.Char(')').Token();
    /// <summary>
    /// {
    /// </summary>
    public static Parser<char> OPENING_CURLY_BRACKET = Parse.Char('{').Token();
    /// <summary>
    /// }
    /// </summary>
    public static Parser<char> CLOSING_CURLY_BRACKET = Parse.Char('}').Token();
    /// <summary>
    /// [
    /// </summary>
    public static Parser<char> OPENING_SQUARE_BRACKET = Parse.Char('[').Token();
    /// <summary>
    /// ]
    /// </summary>
    public static Parser<char> CLOSING_SQUARE_BRACKET = Parse.Char(']').Token();
    /// <summary>
    /// .
    /// </summary>
    public static Parser<char> DOT = Parse.Char('.').Token();
}
