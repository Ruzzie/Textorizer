using System;

namespace Textorizer.Html
{
    internal static class HtmlTagParser
    {
        public static HtmlElementType Parse(in ReadOnlySpan<char> htmlTagStringValue)
        {
            if (htmlTagStringValue.IsEmpty)
            {
                return HtmlElementType.None;
            }

            if (htmlTagStringValue.Length < 3)
            {
                return HtmlElementType.None;
            }

            if (htmlTagStringValue[0] != Scanner.HTML_TAG_START)
            {
                return HtmlElementType.None;
            }

            var startIndex = 1; //"the string always starts with <

            //skip whitespaces
            for (var i = startIndex; i < htmlTagStringValue.Length; i++)
            {
                var currChar = htmlTagStringValue[i];
                if (char.IsWhiteSpace(currChar) || currChar == '/')
                {
                    continue;
                }

                startIndex = i;
                break;
            }

            Span<char> elemName          = stackalloc char[htmlTagStringValue.Length]; // temporary placeholder
            var        elementNameLength = 0;

            //get the first word in the tag
            for (var i = startIndex; i < htmlTagStringValue.Length; i++)
            {
                var currentChar = htmlTagStringValue[i];

                if (currentChar == '/'
                    || char.IsWhiteSpace(currentChar)
                    || currentChar == '>')
                {
                    break;
                }

                elemName[elementNameLength++] = char.ToUpperInvariant(currentChar);
            }

            if (StartsWithDigit(elementNameLength, elemName))
            {
                return HtmlElementType.Invalid;
            }

            return elementNameLength switch
            {
                1 when elemName[0] == 'P' => HtmlElementType.P,
                2 when elemName[0] == 'B' && elemName[1] == 'R' => HtmlElementType.Br,
                2 when elemName[0] == 'U' && elemName[1] == 'L' => HtmlElementType.Ul,
                2 when elemName[0] == 'O' && elemName[1] == 'L' => HtmlElementType.Ol,
                2 when elemName[0] == 'L' && elemName[1] == 'I' => HtmlElementType.Li,
                2 when elemName[0] == 'H' && elemName[1] == 'R' => HtmlElementType.Hr,
                3 when elemName[0] == 'P' && elemName[1] == 'R' && elemName[2] == 'E' => HtmlElementType.Pre,
                6 when elemName[0] == 'S' && elemName[1] == 'C' && elemName[2] == 'R' &&
                       elemName[3] == 'I' && elemName[4] == 'P' &&
                       elemName[5] == 'T' => HtmlElementType.Script,
                5 when elemName[0] == 'S' && elemName[1] == 'T' && elemName[2] == 'Y' &&
                       elemName[3] == 'L' && elemName[4] == 'E' => HtmlElementType.Style,
                _ => HtmlElementType.Other
            };
        }

        private static bool StartsWithDigit(int elementNameLength, in Span<char> elemName)
        {
            return elementNameLength > 0 && char.IsDigit(elemName[0]);
        }
    }
}