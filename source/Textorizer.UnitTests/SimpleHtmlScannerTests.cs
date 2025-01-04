using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Textorizer.Html;

namespace Textorizer.UnitTests;

[TestFixture]
public class SimpleHtmlScannerTests
{
    [Test]
    public void PlainTextSmokeTest()
    {
        //Act
        var sourceScanState = new SourceScanState("hello".AsMemory());
        var token           = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        new string(token.Value(sourceScanState.SourceDataSpan)).Should().Be("hello");
    }

    [Test]
    public void ReturnsEofTokenAsLastToken()
    {
        //Arrange
        var sourceScanState = new SourceScanState("Hello".AsMemory());

        //Act
        new string(Scanner.ScanNextToken(ref sourceScanState).Value(sourceScanState.SourceDataSpan)).Should()
                                                                                                    .Be("Hello");

        var token = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(TokenType.Eof);
    }

    [Test]
    public void ReturnsHtmlTokenAfterInvalidTag()
    {
        //Arrange
        var sourceScanState = new SourceScanState("aaa<1<p>".AsMemory());

        //Act
        //1.
        new string(Scanner.ScanNextToken(ref sourceScanState).Value(sourceScanState.SourceDataSpan)).Should()
                                                                                                    .Be("aaa");

        //2.
        new string(Scanner.ScanNextToken(ref sourceScanState).Value(sourceScanState.SourceDataSpan)).Should()
                                                                                                    .Be("<1");

        //3.
        new string(Scanner.ScanNextToken(ref sourceScanState).Value(sourceScanState.SourceDataSpan)).Should()
                                                                                                    .Be("<p>");
    }

    [TestCase("<p>hello</p")]
    [TestCase("hello</p")]
    [TestCase("<br/>")]
    public void ReturnsEofTokenAsLastTokenForHtml(string input)
    {
        //Arrange
        var   sourceScanState = new SourceScanState(input.AsMemory());
        Token current         = default;

        //Act
        while (current.TokenType != TokenType.Eof)
        {
            //consume
            current = Scanner.ScanNextToken(ref sourceScanState);
        }

        //Assert
        current.TokenType.Should().Be(TokenType.Eof);
    }

    [TestCase("<p>")]
    [TestCase("<P>")]
    [TestCase("<   p>")]
    [TestCase("<p    >")]
    [TestCase("<    p    >")]
    [TestCase("<ul>")]
    [TestCase("<ul class=\"pretty\" data-attrs=\"100\">")]
    [TestCase("<ul\n class=\"pretty\" \n data-attrs=\"100\"\n>")]
    [TestCase("<img src=\"http://test\">")]
    [TestCase("<p id='one'>")]
    [TestCase("<input disabled>")]
    public void ReturnsHtmlOpenTagTokenForHtmlOpenTagInText(string input)
    {
        //Arrange
        var sourceScanState = new SourceScanState(input.AsMemory());

        //Act
        var token = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        token.TokenType.Should()
             .Be(TokenType.HtmlOpenTag, $"for : {token.Value(sourceScanState.SourceDataSpan)}");
        new string(token.Value(sourceScanState.SourceDataSpan)).Should().Be(input);
    }

    [TestCase("</p>")]
    [TestCase("</P>")]
    [TestCase("</   p>")]
    [TestCase("</p    >")]
    [TestCase("</    p    >")]
    [TestCase("</ul>")]
    [TestCase("</img>")]
    [TestCase("</ul class=\"pretty\" data-attrs=\"100\">")]
    [TestCase("</ul\n class=\"pretty\" \n data-attrs=\"100\"\n>")]
    public void ReturnsHtmlCloseTagTokenForHtmlOpenTagInText(string input)
    {
        //Arrange
        var sourceScanState = new SourceScanState(input.AsMemory());

        //Act
        var token = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        token.TokenType.Should()
             .Be(TokenType.HtmlCloseTag, $"for : {token.Value(sourceScanState.SourceDataSpan)}");
        new string(token.Value(sourceScanState.SourceDataSpan)).Should().Be(input);
    }

    [TestCase("<p/>")]
    [TestCase("<P/>")]
    [TestCase("<img src=\"http://test\"/>")]
    [TestCase("<p    />")]
    [TestCase("<    br    />")]
    [TestCase("<    hr    />")]
    [TestCase("<ul/>")]
    [TestCase("<ul class=\"pretty\" data-attrs=\"100\"/>")]
    [TestCase("<ul\n class=\"pretty\" \n data-attrs=\"100\"\n/>")]
    [TestCase("<input disabled async/>")]
    public void ReturnsHtmlSelfClosingTagTokenForHtmlOpenTagInText(string input)
    {
        //Arrange
        var sourceScanState = new SourceScanState(input.AsMemory());

        //Act
        var token = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        token.TokenType.Should()
             .Be(TokenType.HtmlSelfClosingTag, $"for : {token.Value(sourceScanState.SourceDataSpan)}");
        new string(token.Value(sourceScanState.SourceDataSpan)).Should().Be(input);
    }

    [TestCase("text",      TokenType.Text)]
    [TestCase("<open>",    TokenType.HtmlOpenTag)]
    [TestCase("</close>",  TokenType.HtmlCloseTag)]
    [TestCase("<close/>",  TokenType.HtmlSelfClosingTag)]
    [TestCase("&gt;",      TokenType.HtmlEntity)]
    [TestCase("&unknown;", TokenType.HtmlEntity)]
    [TestCase("&#60;",     TokenType.HtmlEntity)]
    [TestCase("&#xE5;",    TokenType.HtmlEntity)]
    [TestCase("&#Xe5;",    TokenType.HtmlEntity)]
    [TestCase("&#Xe5;",    TokenType.HtmlEntity)]
    [TestCase("&#X00;",    TokenType.HtmlEntity)]
    public void TokenTypesTest(string input, TokenType expected)
    {
        //Arrange
        var sourceScanState = new SourceScanState(input.AsMemory());

        //Act
        var token = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        token.TokenType.Should().Be(expected, $"for : {token.Value(sourceScanState.SourceDataSpan)}");
        new string(token.Value(sourceScanState.SourceDataSpan)).Should().Be(input);
    }

    [Test]
    public void EndsInHtmlOpenTagCharacter()
    {
        //Arrange
        var sourceScanState = new SourceScanState("aaa<".AsMemory());

        //Act & Assert
        var current = Scanner.ScanNextToken(ref sourceScanState);
        current.TokenType.Should().Be(TokenType.Text, $"for: {current.Value(sourceScanState.SourceDataSpan)}");
        new string(current.Value(sourceScanState.SourceDataSpan)).Should().Be("aaa");

        current = Scanner.ScanNextToken(ref sourceScanState);
        current.TokenType.Should().Be(TokenType.Text, $"for: {current.Value(sourceScanState.SourceDataSpan)}");
        new string(current.Value(sourceScanState.SourceDataSpan)).Should().Be("<");
    }

    [TestCase("</p>",      HtmlElementType.P)]
    [TestCase("<UL>",      HtmlElementType.Ul)]
    [TestCase("<Li>",      HtmlElementType.Li)]
    [TestCase("<ol>",      HtmlElementType.Ol)]
    [TestCase("< br/>",    HtmlElementType.Br)]
    [TestCase("<hr />",    HtmlElementType.Hr)]
    [TestCase("<u <",      HtmlElementType.Invalid)]
    [TestCase("< < u",     HtmlElementType.Invalid)]
    [TestCase("b>",        HtmlElementType.None)]
    [TestCase("text",      HtmlElementType.None)]
    [TestCase("<1",        HtmlElementType.Invalid)]
    [TestCase("<1>",       HtmlElementType.Invalid)]
    [TestCase("<script>",  HtmlElementType.Script)]
    [TestCase("</script>", HtmlElementType.Script)]
    [TestCase("<script/>", HtmlElementType.Script)]
    [TestCase("<style>",   HtmlElementType.Style)]
    [TestCase("</style>",  HtmlElementType.Style)]
    public void FirstTokenHtmlElementTypeTests(in string input, HtmlElementType expected)
    {
        //Arrange
        var sourceScanState = new SourceScanState(input.AsMemory());

        //Act
        var token = Scanner.ScanNextToken(ref sourceScanState);

        //Assert
        token.HtmlElementType.Should()
             .Be(expected, $"for [{token.TokenType}] : \"{token.Value(sourceScanState.SourceDataSpan)}\"");
    }

    [Test]
    public void BlockLevelHappyPathTests()
    {
        //Act
        var sourceScanState = new SourceScanState("<x><z><a/></z></x>".AsMemory());

        //Assert
        //<x> Open token should be level 0
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);

        //<z> Open token should be level 1
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(1);

        //<a/> Open token should be level 1
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(2);

        //</z> Open token should be level 1
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(1);

        //</x> Open token should be level 0
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);
    }

    [Test]
    public void BlockLevelInvalidHtmlTests()
    {
        //Act
        var sourceScanState = new SourceScanState("<x><a/></b></x>".AsMemory());

        //Assert
        //<x> Open token should be level 0
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);

        //<a/> Open token should be level 1
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(1);

        //</b> Open token should be level 1
        Scanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);

        //</x> Open token should be level 0
        Scanner.ScanNextToken(ref sourceScanState)
               .BlockLevel.Should()
               .Be(-1); //think about what to do with this, could cap it to 0
    }

    [TestCase("<p>",       HtmlElementType.P)]
    [TestCase("<ul>",      HtmlElementType.Ul)]
    [TestCase("<ol>",      HtmlElementType.Ol)]
    [TestCase("<li>",      HtmlElementType.Li)]
    [TestCase("<p/>",      HtmlElementType.P)]
    [TestCase("<  p  />",  HtmlElementType.P)]
    [TestCase("</  p  >",  HtmlElementType.P)]
    [TestCase("</p>",      HtmlElementType.P)]
    [TestCase("<a>",       HtmlElementType.Other)]
    [TestCase("<script>",  HtmlElementType.Script)]
    [TestCase("</script>", HtmlElementType.Script)]
    [TestCase("<script/>", HtmlElementType.Script)]
    [TestCase("<style>",   HtmlElementType.Style)]
    [TestCase("</style>",  HtmlElementType.Style)]
    [TestCase("</pre>",    HtmlElementType.Pre)]
    [TestCase("<pre>",     HtmlElementType.Pre)]
    public void ParseToElementTypeTests(string input, HtmlElementType expected)
    {
        HtmlTagParser.Parse(input).Should().Be(expected);
    }

    [TestCase(1024 * 1024)]
    [TestCase(256 * 1024 - 1)]
    public void ParseToElementTypeNoStackOverflowTest(int strSize)
    {
        var capacity    = strSize;
        var largeString = new StringBuilder(capacity);
        largeString.Append("<t");
        largeString.AppendJoin("", Enumerable.Repeat("h", capacity - 4));
        largeString.Append(">");

        HtmlTagParser.Parse(largeString.ToString()).Should().Be(HtmlElementType.Other);
    }

    [FsCheck.NUnit.Property]
    public void ParseToElementPropertyTests(string input)
    {
        HtmlTagParser.Parse(input);
    }
}