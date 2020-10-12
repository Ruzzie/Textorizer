using System;
using System.Net;
using System.Text;

namespace Textorizer.Html
{
    internal sealed class HtmlToPlainTextWriter : IHtmlToTextWriter
    {
        public void WriteOpenElement(in TextorizeState state, in Token tokenToWrite)
        {
            switch (state.CurrentToken.HtmlElementType)
            {
                case HtmlElementType.Invalid:
                    WriteText(state, tokenToWrite);
                    break;
                case HtmlElementType.None:
                case HtmlElementType.Script:
                case HtmlElementType.Style:
                case HtmlElementType.Other:
                case HtmlElementType.Pre:
                    break;
                case HtmlElementType.P:
                case HtmlElementType.Ul:
                case HtmlElementType.Ol:
                case HtmlElementType.Br:
                case HtmlElementType.Hr:
                    state.Out.Append("\n");
                    break;
                case HtmlElementType.Li:
                    state.Out.Append(new string('\t', Math.Max(state.CurrentBlockDepth - 2, 1)) + "- ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.CurrentToken.HtmlElementType),
                                                          state.CurrentToken.HtmlElementType, null);
            }
        }

        public void WriteCloseElement(in TextorizeState state, in Token tokenToWrite)
        {
            switch (state.CurrentToken.HtmlElementType)
            {
                case HtmlElementType.Invalid:
                    WriteText(state, tokenToWrite);
                    break;
                case HtmlElementType.None:
                case HtmlElementType.Other:
                case HtmlElementType.Script:
                case HtmlElementType.Style:
                case HtmlElementType.Pre:
                case HtmlElementType.Br:
                case HtmlElementType.Hr:
                case HtmlElementType.Ul:
                case HtmlElementType.Ol:
                    break;
                case HtmlElementType.P:
                case HtmlElementType.Li:
                    state.Out.Append("\n");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.CurrentToken.HtmlElementType),
                                                          state.CurrentToken.HtmlElementType, null);
            }
        }

        public void WriteText(in TextorizeState state, in Token tokenToWrite)
        {
            if (tokenToWrite.TokenType == TokenType.HtmlEntity)
            {
                state.Out.Append(WebUtility.HtmlDecode(tokenToWrite.NewValueStr()));
                return;
            }

            if (state.PreviousToken.HtmlElementType == HtmlElementType.Pre &&
                state.PreviousToken.TokenType == TokenType.HtmlOpenTag)
            {
                //In the HTML, a leading newline character immediately following the pre element start tag is stripped
                state.Out.Append(tokenToWrite.NewValueStr().TrimStart('\r', '\n'));
                return;
            }

            if (state.IsInPreformattedText)
            {
                //we are in a preformatted text block(s)
                //   add the original text values with all whitespaces preserved
                state.Out.Append(tokenToWrite.NewValueStr());
                return;
            }

            switch (state.InHtmlElement)
            {
                default:
                    var htmlTrimmedText = ReduceHtmlWhiteSpaces(tokenToWrite.Value.Span);
                    if (EndsOnNewLine(state.Out) || IsFirstTextContentInLi(state, tokenToWrite))
                    {
                        htmlTrimmedText = htmlTrimmedText.TrimStart();
                    }

                    state.Out.Append(htmlTrimmedText);
                    break;
            }
        }

        private static bool IsFirstTextContentInLi(in TextorizeState state, in Token tokenToWrite)
        {
            //when you are the first piece of text content in a Li, al leading whitespaces should be removed
            return state.PreviousToken.HtmlElementType == HtmlElementType.Li &&
                   state.PreviousToken.TokenType == TokenType.HtmlOpenTag
                   && tokenToWrite.TokenType == TokenType.Text;
        }

        public static string ReplaceHtmlWhiteSpacesClassic(string stringValue)
        {
            return stringValue
                   .Replace("  ", " ")
                   .Replace("\r\n ", " ")
                   .Replace("\n ", " ");
        }

        /// reduces all contiguous whitespace characters to a singe space character
        public static string ReduceHtmlWhiteSpaces(ReadOnlySpan<char> stringValue)
        {
            if (stringValue.IsEmpty)
            {
                return string.Empty;
            }

            var        inputLength  = stringValue.Length;
            Span<char> outputBuffer = stackalloc char[inputLength];

            var outputIndex = 0;
            for (var i = 0; i < inputLength; i++)
            {
                var currentInputChar = stringValue[i];

                if (char.IsWhiteSpace(currentInputChar))
                {
                    outputBuffer[outputIndex++] = ' ';
                    var nextIndex = (i + 1);

                    //while !AtEnd && peekNext == whitespace
                    while (nextIndex < inputLength && char.IsWhiteSpace(stringValue[nextIndex]))
                    {
                        //Skip continuous whitespaces
                        i++;
                        nextIndex++;
                    }
                }
                else
                {
                    outputBuffer[outputIndex++] = currentInputChar;
                }
            }

            return new string(outputBuffer[..outputIndex]);
        }

        private static bool EndsOnNewLine(StringBuilder buffer)
        {
            return buffer.Length > 0 && buffer[^1] == '\n';
        }
    }
}