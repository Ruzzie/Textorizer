﻿using System;
using System.Net;
using System.Text;

namespace Textorizer.Html;

internal sealed class PlainTextWriter : IHtmlToTextWriter
{
    public void WriteOpenElement(in TextorizeState state, Token tokenToWrite)
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
            case HtmlElementType.Ol:
            case HtmlElementType.Ul:
                if (state.ListDepth <= 1)
                    state.Out.Append('\n'); //Only write a new line when we are NOT nested in another list
                break;
            case HtmlElementType.P:


            case HtmlElementType.Br:
            case HtmlElementType.Hr:
                state.Out.Append('\n');
                break;
            case HtmlElementType.Li:
                state.Out.Append('\n');
                state.Out.Append('\t', Math.Max(state.ListDepth, 1));
                state.Out.Append("- ");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state.CurrentToken.HtmlElementType)
                                                    , state.CurrentToken.HtmlElementType
                                                    , null);
        }
    }

    public void WriteCloseElement(in TextorizeState state, Token tokenToWrite)
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
                break;
            case HtmlElementType.Ul:
            case HtmlElementType.Ol:
                if (state.ListDepth <= 1)
                    state.Out.Append('\n'); //Only write a new line when we are NOT nested in another list
                break;
            case HtmlElementType.Li:

                break;
            case HtmlElementType.P:

                state.Out.Append("\n");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state.CurrentToken.HtmlElementType)
                                                    , state.CurrentToken.HtmlElementType
                                                    , null);
        }
    }

    public void WriteText(in TextorizeState state, Token tokenToWrite)
    {
        ReadOnlySpan<char> trimChars = stackalloc char[2] { '\r', '\n' };

        if (tokenToWrite.TokenType == TokenType.HtmlEntity)
        {
            state.Out.Append(WebUtility.HtmlDecode(new string(tokenToWrite.Value(state.Source))));
            return;
        }

        if (state.PreviousToken.HtmlElementType == HtmlElementType.Pre &&
            state.PreviousToken.TokenType       == TokenType.HtmlOpenTag)
        {
            //In the HTML, a leading newline character immediately following the pre element start tag is stripped
            state.Out.Append(tokenToWrite.Value(state.Source).TrimStart(trimChars));
            return;
        }

        if (state.IsInPreformattedText)
        {
            //we are in a preformatted text block(s)
            //   add the original text values with all whitespaces preserved
            state.Out.Append(tokenToWrite.Value(state.Source));
            return;
        }

        switch (state.InHtmlElement)
        {
            case HtmlElementType.Other:
            case HtmlElementType.Script:
            case HtmlElementType.Style:
            case HtmlElementType.Pre:
            case HtmlElementType.P:
            case HtmlElementType.Ul:
            case HtmlElementType.Ol:
            case HtmlElementType.Li:
            case HtmlElementType.Br:
            case HtmlElementType.Hr:
                var htmlTrimmedText = ReduceHtmlWhiteSpaces(tokenToWrite.Value(state.Source));

                if (EndsOnNewLine(state.Out) || IsFirstTextContentInLi(state, tokenToWrite))
                {
                    htmlTrimmedText = htmlTrimmedText.TrimStart();
                }

                state.Out.Append(htmlTrimmedText);
                break;
            default:
                if (IsAfterHtmlElement(state))
                {
                    if (EndsOnWhitespace(state.Out))
                        state.Out.Append(tokenToWrite.Value(state.Source).TrimStart());
                    else
                        state.Out.Append(ReduceHtmlWhiteSpaces(tokenToWrite.Value(state.Source)));
                }
                else
                {
                    state.Out.Append(tokenToWrite.Value(state.Source));
                }

                break;
        }
    }

    private static bool IsAfterHtmlElement(TextorizeState state)
    {
        return state.PreviousToken.HtmlElementType != HtmlElementType.None &&
               state.PreviousToken.HtmlElementType != HtmlElementType.Invalid;
    }

    private static bool IsFirstTextContentInLi(TextorizeState state, Token tokenToWrite)
    {
        //when you are the first piece of text content in a Li, all leading whitespaces should be removed
        return state.PreviousToken.HtmlElementType == HtmlElementType.Li &&
               state.PreviousToken.TokenType       == TokenType.HtmlOpenTag
               && tokenToWrite.TokenType           == TokenType.Text;
    }

    public static string ReplaceHtmlWhiteSpacesClassic(string stringValue)
    {
        return stringValue
               .Replace("  ",    " ")
               .Replace("\r\n ", " ")
               .Replace("\n ",   " ");
    }

    /// reduces all contiguous whitespace characters to a singe space character
    public static string ReduceHtmlWhiteSpaces(ReadOnlySpan<char> stringValue)
    {
        if (stringValue.IsEmpty)
        {
            return "";
        }

        var inputLength = stringValue.Length;
        Span<char> outputBuffer = inputLength <= 256
                                      ? stackalloc char[inputLength]
                                      : new char[inputLength];

        int outputLength = ReduceHtmlWhiteSpaces(stringValue, outputBuffer);
        return new string(outputBuffer[..outputLength]);
    }

    /// reduces all contiguous whitespace characters to a singe space character
    /// <returns>Chars written to the given <paramref name="outputBuffer"/></returns>
    public static int ReduceHtmlWhiteSpaces(ReadOnlySpan<char> stringValue, Span<char> outputBuffer)
    {
        if (stringValue.IsEmpty)
        {
            return 0;
        }

        var inputLength = stringValue.Length;
        var outputIndex = 0;

        for (var i = 0; i < inputLength; i++)
        {
            var currentInputChar = stringValue[i];

            if (char.IsWhiteSpace(currentInputChar))
            {
                outputBuffer[outputIndex++] = ' ';
                var nextIndex = i + 1;

                //while !AtEnd && peekNext == whitespace
                while (nextIndex < inputLength && char.IsWhiteSpace(stringValue[nextIndex]))
                {
                    //Skip contiguous whitespaces
                    i++;
                    nextIndex++;
                }
            }
            else
            {
                outputBuffer[outputIndex++] = currentInputChar;
            }
        }

        return outputIndex;
    }

    private static bool EndsOnNewLine(StringBuilder buffer)
    {
        return buffer.Length > 0 && buffer[^1] == '\n';
    }

    private static bool EndsOnWhitespace(StringBuilder buffer)
    {
        return buffer.Length > 0 && char.IsWhiteSpace(buffer[^1]);
    }
}