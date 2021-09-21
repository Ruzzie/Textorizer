using System;

namespace Textorizer.Html
{ //todo: clean-up and refactor code so it is more 'structured', more readable blocks
    internal static class Scanner
    {
        // ReSharper disable InconsistentNaming
        public const char HTML_TAG_START    = '<';
        public const char HTML_TAG_END      = '>';
        public const char HTML_TAG_SLASH    = '/';
        public const char HTML_ENTITY_START = '&';

        public static readonly Token EOF_TOKEN = new Token(TokenType.Eof, ReadOnlyMemory<char>.Empty, 0);
        // ReSharper restore InconsistentNaming

        public static Token ScanNextToken(ref SourceScanState state)
        {
            if (state.IsAtEnd())
            {
                return EOF_TOKEN;
            }

            state.StartIndex = state.CurrentPos;

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

                        while (current != '\0' && char.IsLetterOrDigit(state.PeekNext()))
                        {
                            current = state.Advance();

                            if (current == ';')
                            {
                                //end of entity
                                return CreateToken(TokenType.HtmlEntity, HtmlElementType.None, state);
                            }
                        }

                        //end of contiguous chars, but no ';' found
                        //next non-whitespace character should be a ';'
                        // ex: &gt    ;
                        //            ^
                        current = AdvanceWhiteSpaces(ref state, current);

                        if (state.PeekNext() == ';' /*current == ';'*/)
                        {
                            state.Advance();
                            //end of entity
                            return CreateToken(TokenType.HtmlEntity, HtmlElementType.None, state);
                        }

                        if (!char.IsLetterOrDigit(current))
                        {
                            //Invalid entity, treat as text
                            return CreateToken(TokenType.Text, HtmlElementType.None, state);
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

            // not in a tag and we found a '<'
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

            while (!state.IsAtEnd()
                   && state.PeekNextUnsafe() != HTML_TAG_START
                   && state.PeekNextUnsafe() != HTML_ENTITY_START)
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
            //  and remember the last char before the '>' so we can check later if it is a self closing tag
            //  we skip all attributes and ignore '>' that are in quotes: (for ex. in an attr. value: class=">1")
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
                bool foundEndTag     = false;

                // move position to next valid '</script>'
                while (!state.IsAtEnd()
                       && foundEndTag == false

                )
                {
                    var lookAhead = state.LookAhead("</script>".Length);
                    if (lookAhead == ReadOnlySpan<char>.Empty)
                    {
                        // We are possibly at the end of the input text, so now we exit the loop

                        //Try to advance to the next close tag candidate, any will do,
                        //  since we now know that there is no valid closing tag
                        //    <script> var x = "1" </y>EOF
                        // ------------------------^
                        // note: this also could be the case for a script closing tag with spaces in the name:
                        //       </    script>

                        while(!state.IsAtEnd() && state.PeekNext() != HTML_TAG_START)
                        {
                            state.Advance();
                        }

                        foundEndTag = true;
                    }
                    else
                    {
                        if (lookAhead[0] == HTML_TAG_START    //       <
                            && lookAhead[1] == HTML_TAG_SLASH //       /
                            && char.ToUpperInvariant(lookAhead[2]) == 'S'
                            && char.ToUpperInvariant(lookAhead[3]) == 'C'
                            && char.ToUpperInvariant(lookAhead[4]) == 'R'
                            && char.ToUpperInvariant(lookAhead[5]) == 'I'
                            && char.ToUpperInvariant(lookAhead[6]) == 'P'
                            && char.ToUpperInvariant(lookAhead[7]) == 'T'
                            && lookAhead[8] == HTML_TAG_END //         >
                        )
                        {
                            foundEndTag = true;
                            //The state.Position is now at the start of the closing style tag;
                            //    we could skip it entirely with: // state.Advance("</script>".Length);
                            //    but we don't, since </script> is a html close tag we position the state
                            // so the next token is a HtmlCloseTag of type Style
                        }
                        else
                        {
                            foundEndTag = false;
                            state.Advance();
                        }
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
                        // We are possibly at the end of the input text, so now we exit the loop

                        //Try to advance to the next close tag candidate, any will do,
                        //  since we now know that there is no valid closing tag
                        //    <style> .c{color:'blue'} </y>EOF
                        // ----------------------------^
                        while(!state.IsAtEnd() && state.PeekNext() != HTML_TAG_START)
                        {
                            state.Advance();
                        }

                        foundEndTag = true;
                    }
                    else
                    {
                        if (lookAhead[0] == HTML_TAG_START    //       <
                            && lookAhead[1] == HTML_TAG_SLASH //       /
                            && char.ToUpperInvariant(lookAhead[2]) == 'S'
                            && char.ToUpperInvariant(lookAhead[3]) == 'T'
                            && char.ToUpperInvariant(lookAhead[4]) == 'Y'
                            && char.ToUpperInvariant(lookAhead[5]) == 'L'
                            && char.ToUpperInvariant(lookAhead[6]) == 'E'
                            && lookAhead[7] == HTML_TAG_END //         >
                        )
                        {
                            foundEndTag = true;
                            //The state.Position is now at the start of the closing style tag;
                            //    we could skip it entirely with: // state.Advance("</style>".Length);
                            //    but we don't, since </style> is a html close tag we position the state
                            // so the next token is a HtmlCloseTag of type Style
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
            //Determine and adjust the Block level (nesting depth);
            //  the block level could be used to determine correctness and 'balance' of open / close tags
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