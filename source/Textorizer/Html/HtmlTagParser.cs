using System;

namespace Textorizer.Html;

internal static class HtmlTagParser
{
    public static HtmlElementType Parse(ReadOnlySpan<char> htmlTagStringValue)
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

        //skip leading whitespaces and / of a closing tag
        //   ex. <     /   script>
        //   startIndex ---^
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

        const int MAX_KNOWN_ELEMENT_LENGTH = 6 + 2; //SCRIPT element is longest known tag + self closing / >

        Span<char> uCaseElementNameCandidate = stackalloc char[MAX_KNOWN_ELEMENT_LENGTH]; // temporary placeholder
        var        elementNameLength         = 0;

        //get the first word of max known element length in the tag
        //  and uppercase it for comparison later
        for (var i = startIndex; i < MAX_KNOWN_ELEMENT_LENGTH && i < htmlTagStringValue.Length; i++)
        {
            var currentChar = htmlTagStringValue[i];

            if (currentChar == '/'
                || char.IsWhiteSpace(currentChar)
                || currentChar == '>')
            {
                // element closing '/' and or '>'
                // or a space in the element name is found, we want the part before the space
                //   ex.: script  src="x" async>
                //   breaks ---^
                //   ex.: script/>
                //   breaks ---^
                //   ex.: script>
                //   breaks ---^
                break;
            }

            uCaseElementNameCandidate[elementNameLength++] = char.ToUpperInvariant(currentChar);
        }

        if (StartsWithDigit(elementNameLength, uCaseElementNameCandidate))
        {
            return HtmlElementType.Invalid;
        }

        return elementNameLength switch
        {
            1 when uCaseElementNameCandidate[0] == 'P' => HtmlElementType.P,
            2 when uCaseElementNameCandidate[0] == 'B' && uCaseElementNameCandidate[1] == 'R' => HtmlElementType.Br,
            2 when uCaseElementNameCandidate[0] == 'U' && uCaseElementNameCandidate[1] == 'L' => HtmlElementType.Ul,
            2 when uCaseElementNameCandidate[0] == 'O' && uCaseElementNameCandidate[1] == 'L' => HtmlElementType.Ol,
            2 when uCaseElementNameCandidate[0] == 'L' && uCaseElementNameCandidate[1] == 'I' => HtmlElementType.Li,
            2 when uCaseElementNameCandidate[0] == 'H' && uCaseElementNameCandidate[1] == 'R' => HtmlElementType.Hr,
            3 when uCaseElementNameCandidate[0] == 'P' && uCaseElementNameCandidate[1] == 'R' && uCaseElementNameCandidate[2] == 'E' => HtmlElementType.Pre,
            6 when uCaseElementNameCandidate[0] == 'S' && uCaseElementNameCandidate[1] == 'C' && uCaseElementNameCandidate[2] == 'R' &&
                   uCaseElementNameCandidate[3] == 'I' && uCaseElementNameCandidate[4] == 'P' &&
                   uCaseElementNameCandidate[5] == 'T' => HtmlElementType.Script,
            5 when uCaseElementNameCandidate[0] == 'S' && uCaseElementNameCandidate[1] == 'T' && uCaseElementNameCandidate[2] == 'Y' &&
                   uCaseElementNameCandidate[3] == 'L' && uCaseElementNameCandidate[4] == 'E' => HtmlElementType.Style,
            _ => HtmlElementType.Other
        };
    }

    private static bool StartsWithDigit(int elementNameLength, Span<char> elemName)
    {
        return elementNameLength > 0 && char.IsDigit(elemName[0]);
    }
}