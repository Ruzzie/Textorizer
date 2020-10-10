using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DCrafting.Textorize.Html
{
    public class HtmlToPlainTextWriter : IHtmlToTextWriter
    {
        public void WriteOutputForOpenElement(StringBuilder   buffer,
                                              Token           currentToken,
                                              in int          currentBlockDepth,
                                              HtmlElementType inHtmlElement,
                                              Token           previousToken
        )
        {
            switch (currentToken.HtmlElementType)
            {
                case HtmlElementType.Invalid:
                    WriteText(buffer, currentToken, currentBlockDepth, inHtmlElement, previousToken);
                    break;
                case HtmlElementType.None:
                case HtmlElementType.Script:
                case HtmlElementType.Style:
                case HtmlElementType.Other:
                    break;
                case HtmlElementType.P:
                case HtmlElementType.Ul:
                case HtmlElementType.Ol:
                case HtmlElementType.Br:
                case HtmlElementType.Hr:
                    buffer.Append("\n");
                    break;
                case HtmlElementType.Li:
                    buffer.Append(new string('\t', Math.Max(currentBlockDepth - 2, 1)) + "- ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentToken.HtmlElementType),
                                                          currentToken.HtmlElementType, null);
            }
        }

        public void WriteOutputForCloseElement(StringBuilder   buffer,
                                               Token           currentToken,
                                               in int          currentBlockDepth,
                                               HtmlElementType inHtmlElement,
                                               Token           previousToken)
        {
            switch (currentToken.HtmlElementType)
            {
                case HtmlElementType.Invalid:
                    WriteText(buffer, currentToken, currentBlockDepth, inHtmlElement, previousToken);
                    break;
                case HtmlElementType.None:
                case HtmlElementType.Other:
                case HtmlElementType.Script:
                case HtmlElementType.Style:
                case HtmlElementType.Br:
                case HtmlElementType.Hr:
                case HtmlElementType.Ul:
                case HtmlElementType.Ol:
                    break;
                case HtmlElementType.P:
                case HtmlElementType.Li:
                    buffer.Append("\n");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentToken.HtmlElementType),
                                                          currentToken.HtmlElementType, null);
            }
        }

        public void WriteText(StringBuilder   buffer,
                              Token           currentToken,
                              in int          currentBlockDepth,
                              HtmlElementType inHtmlElement,
                              Token           previousToken)
        {

            if (currentToken.TokenType == TokenType.HtmlEntity)
            {
                buffer.Append(WebUtility.HtmlDecode(currentToken.NewValueStr()));
                return;
            }

            switch (inHtmlElement)
            {
                //TODO: trim end on text is Li when it is the only token in the Li
                case HtmlElementType.Li:
                case HtmlElementType.P:
                    if (previousToken.TokenType == TokenType.Text ||
                        previousToken.HtmlElementType == HtmlElementType.Invalid ||
                        previousToken.HtmlElementType == HtmlElementType.None)
                        buffer.Append(currentToken.NewValueStr());
                    else
                        buffer.Append(currentToken.NewValueStr().TrimStart());
                    break;

                /*if (previousToken.TokenType == TokenType.Text ||
                    previousToken.HtmlElementType == HtmlElementType.Invalid ||
                    previousToken.HtmlElementType == HtmlElementType.None)

                    buffer.Append(WebUtility.HtmlDecode(currentToken.Value));
                else
                    buffer.Append(WebUtility.HtmlDecode(currentToken.Value.Trim()));
                break;*/

                case HtmlElementType.None:
                    var htmlTrimmedText = ReplaceHtmlWhiteSpacesNew(currentToken.Value.Span);
                    if (EndsOnNewLine(buffer))
                    {
                        htmlTrimmedText = htmlTrimmedText.TrimStart();
                    }

                    buffer.Append(htmlTrimmedText);

                    break;
                default:
                    buffer.Append(currentToken.NewValueStr());
                    break;
            }
        }

        public static string ReplaceHtmlWhiteSpacesClassic(string stringValue)
        {
            return stringValue
                   .Replace("  ", " ")
                   .Replace("\r\n ", " ")
                   .Replace("\n ", " ");
        }

        /// reduces all contiguous whitespaces to a singe space character
        public static string ReplaceHtmlWhiteSpacesNew(ReadOnlySpan<char> stringValue)
        {
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

                    while (nextIndex < inputLength && char.IsWhiteSpace(stringValue[nextIndex])) //peek next
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

            return new string(outputBuffer[0..outputIndex]);
        }

        private static bool EndsOnNewLine(StringBuilder buffer)
        {
            return buffer.Length > 0 && buffer[^1] == '\n';
        }
    }
}