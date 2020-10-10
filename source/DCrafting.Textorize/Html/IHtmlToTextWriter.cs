using System.Text;

namespace DCrafting.Textorize.Html
{
    public interface IHtmlToTextWriter
    {
        void WriteText(StringBuilder   buffer,
                       Token           currentToken,
                       in int          currentBlockDepth,
                       HtmlElementType inHtmlElement,
                       Token           previousToken);

        void WriteOutputForOpenElement(StringBuilder   buffer,
                                       Token           currentToken,
                                       in int          currentBlockDepth,
                                       HtmlElementType currentBlockElem,
                                       Token           previousToken
                                      );

        void WriteOutputForCloseElement(StringBuilder   buffer,
                                        Token           currentToken,
                                        in int          currentBlockDepth,
                                        HtmlElementType currentBlockElem,
                                        Token           previousToken);
    }
}