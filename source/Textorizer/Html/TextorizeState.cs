using System;
using System.Text;

namespace Textorizer.Html;

internal ref struct TextorizeState
{
    public readonly StringBuilder      Out;
    public          HtmlElementType    InHtmlElement;
    public          int                CurrentBlockDepth;
    public          Token              PreviousToken;
    public          Token              CurrentToken;
    public          int                PreDepth;
    public          int                ListDepth;
    public          ReadOnlySpan<char> Source;

    public bool IsInPreformattedText => PreDepth > 0;

    public TextorizeState(StringBuilder      @out
                        , Token              currentToken
                        , int                currentBlockDepth
                        , HtmlElementType    inHtmlElement
                        , Token              previousToken
                        , ReadOnlySpan<char> source)
    {
        Out               = @out;
        CurrentToken      = currentToken;
        CurrentBlockDepth = currentBlockDepth;
        InHtmlElement     = inHtmlElement;
        PreviousToken     = previousToken;
        Source            = source;
        PreDepth          = 0;
        ListDepth         = 0;
    }
}