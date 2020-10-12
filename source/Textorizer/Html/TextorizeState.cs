using System.Text;

namespace Textorizer.Html
{
    internal ref struct TextorizeState
    {
        public readonly StringBuilder   Out;
        public          HtmlElementType InHtmlElement;
        public          int             CurrentBlockDepth;
        public          Token           PreviousToken;
        public          Token           CurrentToken;
        public          int             PreDepth;

        public bool IsInPreformattedText => PreDepth > 0;

        public TextorizeState(StringBuilder   @out,
                             Token           currentToken,
                             int             currentBlockDepth,
                             HtmlElementType inHtmlElement,
                             Token           previousToken)
        {
            Out               = @out;
            CurrentToken      = currentToken;
            CurrentBlockDepth = currentBlockDepth;
            InHtmlElement     = inHtmlElement;
            PreviousToken     = previousToken;
            PreDepth          = 0;
        }
    }
}