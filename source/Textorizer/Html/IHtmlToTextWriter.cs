namespace Textorizer.Html
{
    internal interface IHtmlToTextWriter
    {
        void WriteText(in         TextorizeState state, in Token tokenToWrite);
        void WriteOpenElement(in  TextorizeState state, in Token tokenToWrite);
        void WriteCloseElement(in TextorizeState state, in Token tokenToWrite);
    }
}