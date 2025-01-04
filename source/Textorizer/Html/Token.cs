using System;

namespace Textorizer.Html;

internal readonly struct Token
{
    public readonly TokenType       TokenType;
    public readonly HtmlElementType HtmlElementType;
    public readonly int             BlockLevel;
    public readonly int             ValueStartIndex;
    public readonly int             ValueLength;


    public Token(TokenType       tokenType
               , int             valueStartIndex
               , int             valueLength
               , int             blockLevel
               , HtmlElementType htmlElementType = HtmlElementType.None)
    {
        ValueStartIndex = valueStartIndex;
        ValueLength     = valueLength;
        TokenType       = tokenType;
        BlockLevel      = blockLevel;

        HtmlElementType = htmlElementType;
    }

    /*internal ReadOnlySpan<char> NewValueStr(ReadOnlySpan<char> sourceData)
    {
        return Value(sourceData);
    }*/

    internal ReadOnlySpan<char> Value(ReadOnlySpan<char> sourceData)
    {
        return sourceData.Slice(ValueStartIndex, ValueLength);
    }
}