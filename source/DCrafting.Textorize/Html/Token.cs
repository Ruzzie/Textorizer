using System;

namespace DCrafting.Textorize.Html
{
    public readonly struct Token
    {
        public TokenType       TokenType       { get; }
        public HtmlElementType HtmlElementType { get; }
        public int             BlockLevel      { get; }

        public readonly ReadOnlyMemory<char> Value;

        public string NewValueStr()
        {
            return new string(Value.Span);
        }

        public Token(TokenType            tokenType,
                     ReadOnlyMemory<char> textValue,
                     int                  blockLevel,
                     HtmlElementType      htmlElementType = HtmlElementType.None)
        {
            TokenType       = tokenType;
            BlockLevel      = blockLevel;
            Value           = textValue;
            HtmlElementType = htmlElementType;
        }
    }
}