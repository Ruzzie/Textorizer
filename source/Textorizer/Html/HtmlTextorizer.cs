using System;
using System.Collections.Generic;
using System.Text;

namespace Textorizer.Html
{
    internal sealed class HtmlTextorizer : ITextorizer
    {
        private readonly IHtmlToTextWriter _outputWriter;

        public HtmlTextorizer(IHtmlToTextWriter outputWriter)
        {
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
        }

        public string Textorize(string htmlInput)
        {
            if (string.IsNullOrWhiteSpace(htmlInput))
            {
                return "";
            }

            var source       = new SourceScanState(htmlInput.AsMemory());
            var htmlTagStack = new Stack<TokenInfo>(32);
            var state = new TextorizeState(new StringBuilder(htmlInput.Length),
                                           default,
                                           0,
                                           HtmlElementType.None,
                                           default);

            while ((state.CurrentToken = Scanner.ScanNextToken(ref source)).TokenType != TokenType.Eof)
            {
                switch (state.CurrentToken.TokenType)
                {
                    case TokenType.HtmlEntity:
                    case TokenType.Text:
                        _outputWriter.WriteText(state, state.CurrentToken);
                        break;
                    case TokenType.HtmlOpenTag:

                        if (state.CurrentToken.HtmlElementType != HtmlElementType.Invalid)
                        {
                            state.CurrentBlockDepth++;
                            state.InHtmlElement = state.CurrentToken.HtmlElementType;
                            htmlTagStack.Push(new TokenInfo(state.CurrentBlockDepth, state.CurrentToken));

                            if (state.CurrentToken.HtmlElementType == HtmlElementType.Pre)
                            {
                                state.PreDepth++;
                            }
                            else if (state.CurrentToken.HtmlElementType == HtmlElementType.Ul ||
                                     state.CurrentToken.HtmlElementType == HtmlElementType.Ol)
                            {
                                state.ListDepth++;
                            }
                        }

                        _outputWriter.WriteOpenElement(state, state.CurrentToken);
                        break;
                    case TokenType.HtmlCloseTag:
                        HandleCloseTag(htmlTagStack, ref state);
                        if (state.CurrentToken.HtmlElementType == HtmlElementType.Pre)
                        {
                            state.PreDepth--;
                        }
                        else if (state.CurrentToken.HtmlElementType == HtmlElementType.Ul ||
                                 state.CurrentToken.HtmlElementType == HtmlElementType.Ol)
                        {
                            state.ListDepth--;
                        }

                        break;
                    case TokenType.HtmlSelfClosingTag:
                        _outputWriter.WriteOpenElement(state, state.CurrentToken);
                        break;
                    case TokenType.Eof:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                state.PreviousToken = state.CurrentToken;
            }

            return state.Out.ToString();
        }

        private void HandleCloseTag(Stack<TokenInfo> htmlTagStack, ref TextorizeState state)
        {
            do
            {
                if (htmlTagStack.TryPeek(out var topOfStackTokenInfo))
                {
                    if (IsMatchingCloseTagFor(topOfStackTokenInfo, state.CurrentToken, state.CurrentBlockDepth))
                    {
                        //We have a match
                        //we can pop and discard the open tag op top of the stack
                        htmlTagStack.Pop();
                        _outputWriter.WriteCloseElement(state, state.CurrentToken);
                        state.CurrentBlockDepth--;

                        //if there is a parent, set the InHtmlElementState to the parent's element type
                        SetInHtmlElementToParentOrNone(htmlTagStack, ref state);
                        break;
                    }

                    if (IsOtherHtmlOpenElement(topOfStackTokenInfo, state.CurrentToken))
                    {
                        //I'm a closing tag, but on top of the stack is a DIFFERENT opening tag (invalid html)
                        // <p> </div> </p>
                        //     -^^^^
                        //On top of the stack is an Open tag
                        // We are a Close tag
                        // We do not match
                        if (topOfStackTokenInfo.ElementDepth == state.CurrentBlockDepth) // Same Depth
                        {
                            //<p><p></div></a></p>
                            //       ^^^^   ^
                            var orphanedOpenTag = htmlTagStack.Pop();
                            _outputWriter.WriteCloseElement(state, orphanedOpenTag.Token);
                            state.CurrentBlockDepth--;

                            //if there is a parent, set the InHtmlElementState to the parent's element type
                            SetInHtmlElementToParentOrNone(htmlTagStack, ref state);

                            _outputWriter.WriteCloseElement(state, state.CurrentToken);
                            break;
                        }
                    }
                }
                else
                {
                    //Orphaned close element
                    //Nothing on the stack
                    _outputWriter.WriteCloseElement(state, state.CurrentToken);
                }
            } while (htmlTagStack.Count > 0);
        }

        private static void SetInHtmlElementToParentOrNone(Stack<TokenInfo> htmlTagStack, ref TextorizeState state)
        {
            if (htmlTagStack.TryPeek(out var parent))
                state.InHtmlElement = parent.Token.HtmlElementType;
            else
                state.InHtmlElement = HtmlElementType.None;
        }

        private static bool IsOtherHtmlOpenElement(TokenInfo possibleOpenElement, Token closeElement)
        {
            return possibleOpenElement.Token.TokenType == TokenType.HtmlOpenTag &&
                   possibleOpenElement.Token.HtmlElementType != HtmlElementType.None &&
                   possibleOpenElement.Token.HtmlElementType != HtmlElementType.Invalid &&
                   possibleOpenElement.Token.HtmlElementType != closeElement.HtmlElementType;
        }

        private static bool IsMatchingCloseTagFor(TokenInfo openTagToken,
                                                  Token     currentClosingTagToken,
                                                  int       currentBlockDepth)
        {
            return openTagToken.Token.HtmlElementType == currentClosingTagToken.HtmlElementType
                   && openTagToken.Token.HtmlElementType != HtmlElementType.None
                   && openTagToken.Token.HtmlElementType != HtmlElementType.Invalid
                   && openTagToken.Token.TokenType == TokenType.HtmlOpenTag
                   && currentBlockDepth == openTagToken.ElementDepth;
        }
    }
}