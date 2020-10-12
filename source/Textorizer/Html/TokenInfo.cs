namespace Textorizer.Html
{
    internal readonly struct TokenInfo
    {
        public readonly int   ElementDepth;
        public readonly Token Token;

        public TokenInfo(int elementDepth, in Token token)
        {
            ElementDepth = elementDepth;
            Token        = token;
        }
    }
}