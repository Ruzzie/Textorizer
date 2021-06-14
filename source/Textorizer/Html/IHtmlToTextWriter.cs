namespace Textorizer.Html
{
    internal interface IHtmlToTextWriter
    {
        void WriteText(in         TextorizeState state, Token tokenToWrite);
        void WriteOpenElement(in  TextorizeState state, Token tokenToWrite);
        void WriteCloseElement(in TextorizeState state, Token tokenToWrite);
    }
}