using System;
using System.Collections.Generic;
using System.Text;

namespace DCrafting.Textorize.Html
{
    public class HtmlTextorizer : ITextorizer
    {
        private readonly IHtmlToTextWriter _outputWriter;

        public HtmlTextorizer(IHtmlToTextWriter outputWriter)
        {
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
        }

        public string Textorize(in string possibleHtmlStringInput)
        {
            if (string.IsNullOrWhiteSpace(possibleHtmlStringInput))
            {
                return string.Empty;
            }

            var scanState         = new SourceScanState(possibleHtmlStringInput.AsMemory());
            var outputText        = new StringBuilder(possibleHtmlStringInput.Length);
            var tokenStack        = new Stack<TokenInfo>(32);
            var currentBlockDepth = 0;
            var currentBlockElem  = HtmlElementType.None;

            Token previousToken = default;
            Token currentToken;

            while ((currentToken = SimpleHtmlScanner.ScanNextToken(ref scanState)).TokenType != TokenType.Eof)
            {
                switch (currentToken.TokenType)
                {
                    case TokenType.HtmlEntity:
                    case TokenType.Text:
                        _outputWriter.WriteText(outputText, currentToken, currentBlockDepth, currentBlockElem,
                                                previousToken);
                        break;
                    case TokenType.HtmlOpenTag:

                        if (currentToken.HtmlElementType != HtmlElementType.Invalid)
                        {
                            currentBlockDepth++;
                            currentBlockElem = currentToken.HtmlElementType;
                            tokenStack.Push(new TokenInfo(currentBlockDepth, currentToken));
                        }

                        _outputWriter.WriteOutputForOpenElement(outputText,
                                                                currentToken,
                                                                currentBlockDepth,
                                                                currentBlockElem,
                                                                previousToken
                                                               );

                        break;
                    case TokenType.HtmlCloseTag:
                        HandleCloseTag(tokenStack, currentToken, outputText, ref currentBlockDepth,
                                       ref currentBlockElem, previousToken);
                        break;
                    case TokenType.HtmlSelfClosingTag:
                        _outputWriter.WriteOutputForOpenElement(outputText, currentToken, currentBlockDepth,
                                                                currentBlockElem, previousToken);
                        break;
                    case TokenType.Eof:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                previousToken = currentToken;
            }

            return outputText.ToString();
        }

        private void HandleCloseTag(Stack<TokenInfo>    tokenStack,
                                    in Token            currentToken,
                                    StringBuilder       outputText,
                                    ref int             currentBlockDepth,
                                    ref HtmlElementType currentBlockElem,
                                    in  Token           previousToken)
        {
            do
            {
                if (tokenStack.TryPeek(out var topOfStackTokenInfo))
                {
                    if (IsMatchingCloseTagFor(topOfStackTokenInfo, currentToken, currentBlockDepth))
                    {
                        //We have a match
                        //we can pop and discard the open tag op top of the stack
                        tokenStack.Pop();
                        _outputWriter.WriteOutputForCloseElement(outputText, currentToken, currentBlockDepth,
                                                                 currentBlockElem, previousToken);
                        currentBlockDepth--;

                        //peek if there is a parent
                        if (tokenStack.TryPeek(out var parent))
                            currentBlockElem = parent.Token.HtmlElementType;
                        else
                            currentBlockElem = HtmlElementType.None;

                        break;
                    }

                    if (IsOtherHtmlOpenElement(topOfStackTokenInfo, currentToken))
                    {
                        //I'm a closing tag, but on top of the stack is a DIFFERENT opening tag (invalid html)
                        // <p> </div> </p>
                        //     -^^^^
                        //On top of the stack is an Open tag
                        // We are a Close tag
                        // We do not match
                        if (topOfStackTokenInfo.ElementDepth == currentBlockDepth) // Same Depth
                        {
                            //<p><p></div></a></p>
                            var orphanedOpenTag = tokenStack.Pop();
                            _outputWriter.WriteOutputForCloseElement(outputText, orphanedOpenTag.Token,
                                                                     currentBlockDepth, currentBlockElem,
                                                                     previousToken);
                            currentBlockDepth--;

                            if (tokenStack.TryPeek(out var parent))
                                currentBlockElem = parent.Token.HtmlElementType;
                            else
                                currentBlockElem = HtmlElementType.None;

                            _outputWriter.WriteOutputForCloseElement(outputText, currentToken, currentBlockDepth,
                                                                     currentBlockElem, previousToken);
                            break;
                        }
                    }
                }
                else
                {
                    //Orphaned close element
                    //Nothing on the stack
                    _outputWriter.WriteOutputForCloseElement(outputText, currentToken,
                                                             currentBlockDepth,
                                                             currentBlockElem, previousToken);
                }
            } while (tokenStack.Count > 0);
        }

        private static bool IsOtherHtmlOpenElement(in TokenInfo possibleOpenElement, in Token closeElement)
        {
            return possibleOpenElement.Token.TokenType == TokenType.HtmlOpenTag &&
                   possibleOpenElement.Token.HtmlElementType != HtmlElementType.None &&
                   possibleOpenElement.Token.HtmlElementType != HtmlElementType.Invalid &&
                   possibleOpenElement.Token.HtmlElementType != closeElement.HtmlElementType;
        }

        private static bool IsMatchingCloseTagFor(in TokenInfo openTagToken,
                                                  in Token     currentClosingTagToken,
                                                  int          currentBlockDepth)
        {
            return openTagToken.Token.HtmlElementType == currentClosingTagToken.HtmlElementType
                   && openTagToken.Token.HtmlElementType != HtmlElementType.None
                   && openTagToken.Token.HtmlElementType != HtmlElementType.Invalid
                   && openTagToken.Token.TokenType == TokenType.HtmlOpenTag
                   && currentBlockDepth == openTagToken.ElementDepth;
        }
    }
}