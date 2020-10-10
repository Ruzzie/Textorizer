using System;

namespace DCrafting.Textorize.Html
{
    internal static class SimpleHtmlScanner
    {
        // ReSharper disable InconsistentNaming
        public const char HTML_TAG_START    = '<';
        public const char HTML_TAG_END      = '>';
        public const char HTML_TAG_SLASH    = '/';
        public const char HTML_ENTITY_START = '&';

        public static Token EOF_TOKEN => new Token(TokenType.Eof, ReadOnlyMemory<char>.Empty, 0);
        // ReSharper restore InconsistentNaming

        public static Token ScanNextToken(ref SourceScanState state)
        {
            state.StartIndex = state.CurrentPos;

            if (state.IsAtEnd())
            {
                return EOF_TOKEN;
            }

            var current = state.Advance();

            // A '&' in plain text
            //Possible HTML entity
            // hello &gt; world
            //       ^
            if (current == HTML_ENTITY_START)
            {
                //a valid entity ends on ';'
                // the only spaces that are allowed are before the terminating ';'
                // next should be either a letter or a '#'
                var next = state.PeekNext();
                if (char.IsLetter(next) || next == '#')
                {
                    state.Advance();

                    next = state.PeekNext();
                    if (char.IsLetterOrDigit(next))
                    {
                        //&[a-z][a-z] &#2 or &#x
                        //        ^     ^      ^
                        current = state.Advance();
                        while (current != '\0' &&  !char.IsWhiteSpace(current))
                        {
                            if (current == ';')
                            {
                                //end of entity
                                return CreateToken(TokenType.HtmlEntity, HtmlElementType.None, state);
                            }
                            current = state.Advance();
                        }
                        //end of contiguous chars, but no ';' found
                        //next non-whitespace character should be a ';'
                        // ex: &gt    ;
                        //        ^
                        current = AdvanceWhiteSpaces(ref state, current);
                        if (current == ';')
                        {
                            //end of entity
                            return CreateToken(TokenType.HtmlEntity, HtmlElementType.None, state);
                        }
                        // else it is not a valid entity and we can ignore it
                    }
                }
            }

            if (current == HTML_TAG_START)
            {
                current = state.Advance();
                if (state.IsInTag)
                {
                    //We found a < inside a tag, ex: "<u<p>>
                    //                                  ^
                    //We found a < inside a tag, ex: "<<p>>
                    //                                 ^
                    //Finish the current Tag and start a new one
                    return CreateToken(TokenType.HtmlOpenTag, HtmlElementType.Invalid, state);
                }

                if (state.IsAtEnd())
                {
                    //we ended on a '<' character
                    return CreateToken(TokenType.Text, HtmlElementType.Invalid, state);
                }

                state.IsInTag = true;
            }

            if (state.IsInTag)
            {
                return ScanTag(ref state, current);
            }

            if (current == HTML_TAG_END)
            {
                if (state.IsInTag)
                {
                    state.IsInTag = false;
                }

                //hello > world
                //      ^
                return CreateToken(TokenType.Text, HtmlElementType.Invalid, state);
            }

            // Text content until '<' or '&'
            while (!state.IsAtEnd()
                   && (state.PeekNextUnsafe() != HTML_TAG_START && state.PeekNextUnsafe() != HTML_ENTITY_START))
            {
                state.Advance();
            }

            return CreateToken(TokenType.Text, HtmlElementType.None, state);
        }

        private static Token CreateToken(TokenType tokenType, HtmlElementType htmlElementType, in SourceScanState state)
        {
            return new Token(tokenType,
                             state.SourceData[state.StartIndex .. state.CurrentPos],
                             state.BlockLevel,
                             htmlElementType
                            );
        }

        private static Token ScanTag(ref SourceScanState state, char current)
        {
            var currTokenType = TokenType.HtmlOpenTag;

            //We are in a tag, so continue scan!
            //Ignore all whitespaces after '<' in a tag
            current = AdvanceWhiteSpaces(ref state, current);

            if (current == HTML_TAG_SLASH)
            {
                //We found a '/' after '<' and before the tag name
                // for ex: < / br>
                //           ^
                //This is a close tag
                currTokenType = TokenType.HtmlCloseTag;
            }

            //Next is the element name and attrs until either '/' or '>'
            var lastNonWhitespaceCharacter = '\0';

            var isInQuote = false;

            //Fast forward to the expected '>'
            // and remember the last char before the '>' so we can check later if it is a self closing tag
            //  we skip all attributes and ignore '>' that are in quotes (for ex. in an attr. value: class=">1")
            while (!state.IsAtEnd()
                   && (state.PeekNext() != HTML_TAG_END || isInQuote)
            )
            {
                if (state.PeekNext() == HTML_TAG_START && !isInQuote)
                {
                    //We found a '<' after '<x '
                    // for ex: <1<br>
                    //           ^
                    //Treat this as text and exit
                    state.IsInTag = false;
                    return CreateToken(TokenType.Text, HtmlElementType.Invalid, state);
                }

                current = state.Advance();

                if (!char.IsWhiteSpace(current))
                {
                    lastNonWhitespaceCharacter = current;
                    if (current == '"')
                    {
                        isInQuote = !isInQuote; //toggle
                    }
                }
            }

            //The next character is the '>' char
            // Are we a self-closing tag?
            if (lastNonWhitespaceCharacter == HTML_TAG_SLASH)
            {
                currTokenType = TokenType.HtmlSelfClosingTag;
            }

            state.IsInTag = false;

            if (state.IsAtEnd())
            {
                //We expect the '>' as the next char but we are at the end
                // so for example "<a"
                //                  ^
                return CreateToken(currTokenType,
                                   HtmlElementType.Invalid,
                                   state
                                  );
            }

            state.Advance(); //== '>'; we could double check this...

            //We have reached the close '>' of the tag
            var textValueRangeForTokenInSource = state.StartIndex .. state.CurrentPos;

            var htmlElementType = HtmlTagParser.Parse(state.SourceDataSpan[textValueRangeForTokenInSource]);

            //<script>
            if (currTokenType == TokenType.HtmlOpenTag
                && htmlElementType == HtmlElementType.Script)
            {
                //Skip the contents of the <script> tag
                // move position to next valid '<'
                var isInSingleQuote = false;
                var isInDoubleQuote = false;

                while (!state.IsAtEnd()
                       && ((isInDoubleQuote || isInSingleQuote)
                           || (state.PeekNext() != HTML_TAG_START))
                )
                {
                    current = state.Advance();

                    if (current == '"' && isInSingleQuote == false)
                    {
                        isInDoubleQuote = !isInDoubleQuote; //toggle
                    }
                    else if (current == '\'' && isInDoubleQuote == false)
                    {
                        isInSingleQuote = !isInSingleQuote; //toggle
                    }
                }

                goto RETURN_TOKEN;
            }

            //<style>
            if (currTokenType == TokenType.HtmlOpenTag
                && htmlElementType == HtmlElementType.Style)
            {
                //Skip the contents of the <style> tag
                // move position to the closing tag '</style>'
                bool foundEndTag = false;

                while (!state.IsAtEnd() && foundEndTag == false)
                {
                    var lookAhead = state.LookAhead("</style>".Length);
                    if (lookAhead == ReadOnlySpan<char>.Empty)
                    {
                        //we are at the end of the input text, so now we exit the loop
                        foundEndTag = true;
                    }
                    else
                    {
                        if (lookAhead[0] == HTML_TAG_START
                            && lookAhead[1] == HTML_TAG_SLASH
                            && char.ToUpperInvariant(lookAhead[2]) == 'S'
                            && char.ToUpperInvariant(lookAhead[3]) == 'T'
                            && char.ToUpperInvariant(lookAhead[4]) == 'Y'
                            && char.ToUpperInvariant(lookAhead[5]) == 'L'
                            && char.ToUpperInvariant(lookAhead[6]) == 'E'
                            && lookAhead[7] == HTML_TAG_END
                        )
                        {
                            foundEndTag = true;
                            //The state.Position is now at the start of the closing style tag;
                            //    we could skip it entirely with: // state.Advance("</style>".Length);
                        }
                        else
                        {
                            foundEndTag = false;
                            state.Advance();
                        }
                    }
                }
            }


            RETURN_TOKEN:
            //Determine and adjust the Block level (nesting depth); block level could be used to determine correctness and 'balance' of open / close tags
            var blockLevelForToken = AdjustBlockLevel(ref state, htmlElementType, currTokenType);

            //: so return the token
            var tokenToReturn = new Token(currTokenType,
                                          state.SourceData[textValueRangeForTokenInSource],
                                          blockLevelForToken,
                                          htmlElementType);

            return tokenToReturn;
        }

        private static int AdjustBlockLevel(ref SourceScanState state,
                                            HtmlElementType     htmlElementType,
                                            TokenType           currTokenType)
        {
            return htmlElementType == HtmlElementType.Invalid
                ? state.BlockLevel
                : currTokenType switch
                {
                    TokenType.HtmlOpenTag => state.BlockLevel++,
                    TokenType.HtmlCloseTag => --state.BlockLevel,
                    _ => state.BlockLevel
                };
        }

        private static char AdvanceWhiteSpaces(ref SourceScanState state, char current)
        {
            while (char.IsWhiteSpace(current))
            {
                current = state.Advance();
            }

            return current;
        }
    }
}