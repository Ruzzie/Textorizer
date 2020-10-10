using System;
using DCrafting.Textorize;
using DCrafting.Textorize.Html;
using FluentAssertions;
using NUnit.Framework;

namespace DCrafting.Textorize.UnitTests
{
    [TestFixture]
    public class SimpleHtmlScannerTests
    {
        [Test]
        public void PlainTextSmokeTest()
        {
            //Act
            var sourceScanState = new SourceScanState("hello".AsMemory());
            var token           = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

            //Assert
            token.NewValueStr().Should().Be("hello");
        }

        [Test]
        public void ReturnsEofTokenAsLastToken()
        {
            //Arrange
            var sourceScanState = new SourceScanState("Hello".AsMemory());

            //Act
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).NewValueStr().Should().Be("Hello");

            var token = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

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
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).NewValueStr().Should().Be("aaa");

            //2.
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).NewValueStr().Should().Be("<1");

            //3.
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).NewValueStr().Should().Be("<p>");

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
                current = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);
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
        public void ReturnsHtmlOpenTagTokenForHtmlOpenTagInText(string input)
        {
            //Arrange
            var sourceScanState = new SourceScanState(input.AsMemory());

            //Act
            var token           = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

            //Assert
            token.TokenType.Should().Be(TokenType.HtmlOpenTag, $"for : {token.NewValueStr()}");
            token.NewValueStr().Should().Be(input);
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
            var token = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

            //Assert
            token.TokenType.Should().Be(TokenType.HtmlCloseTag, $"for : {token.NewValueStr()}");
            token.NewValueStr().Should().Be(input);
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
        public void ReturnsHtmlSelfClosingTagTokenForHtmlOpenTagInText(string input)
        {
            //Arrange
            var sourceScanState = new SourceScanState(input.AsMemory());

            //Act
            var token = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

            //Assert
            token.TokenType.Should().Be(TokenType.HtmlSelfClosingTag, $"for : {token.NewValueStr()}");
            token.NewValueStr().Should().Be(input);
        }

        [TestCase("text", TokenType.Text)]
        [TestCase("<open>", TokenType.HtmlOpenTag)]
        [TestCase("</close>", TokenType.HtmlCloseTag)]
        [TestCase("<close/>", TokenType.HtmlSelfClosingTag)]

        [TestCase("&gt;", TokenType.HtmlEntity)]
        [TestCase("&unknown;", TokenType.HtmlEntity)]
        [TestCase("&#60;", TokenType.HtmlEntity)]
        [TestCase("&#xE5;", TokenType.HtmlEntity)]
        [TestCase("&#Xe5;", TokenType.HtmlEntity)]
        [TestCase("&#Xe5;", TokenType.HtmlEntity)]
        [TestCase("&#X00;", TokenType.HtmlEntity)]
        public void TokenTypesTest(string input, TokenType expected)
        {
            //Arrange
            var sourceScanState = new SourceScanState(input.AsMemory());

            //Act
            var token = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

            //Assert
            token.TokenType.Should().Be(expected, $"for : {token.NewValueStr()}");
            token.NewValueStr().Should().Be(input);
        }

        [Test]
        public void EndsInHtmlOpenTagCharacter()
        {
            //Arrange
            var sourceScanState = new SourceScanState("aaa<".AsMemory());

            //Act & Assert
            var current = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);
            current.TokenType.Should().Be(TokenType.Text, $"for: {current.NewValueStr()}");
            current.NewValueStr().Should().Be("aaa");

            current = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);
            current.TokenType.Should().Be(TokenType.Text, $"for: {current.NewValueStr()}");
            current.NewValueStr().Should().Be("<");
        }

        [TestCase("</p>", HtmlElementType.P)]
        [TestCase("<UL>", HtmlElementType.Ul)]
        [TestCase("<Li>", HtmlElementType.Li)]
        [TestCase("<ol>", HtmlElementType.Ol)]
        [TestCase("< br/>", HtmlElementType.Br)]
        [TestCase("<hr />", HtmlElementType.Hr)]
        [TestCase("<u <", HtmlElementType.Invalid)]
        [TestCase("< < u", HtmlElementType.Invalid)]
        [TestCase("b>", HtmlElementType.None)]
        [TestCase("text", HtmlElementType.None)]
        [TestCase("<1", HtmlElementType.Invalid)]
        [TestCase("<1>", HtmlElementType.Invalid)]
        [TestCase("<script>", HtmlElementType.Script)]
        [TestCase("</script>", HtmlElementType.Script)]
        [TestCase("<script/>", HtmlElementType.Script)]
        [TestCase("<style>", HtmlElementType.Style)]
        [TestCase("</style>", HtmlElementType.Style)]
        public void FirstTokenHtmlElementTypeTests(in string input, HtmlElementType expected)
        {
            //Arrange
            var sourceScanState = new SourceScanState(input.AsMemory());

            //Act
            var token = SimpleHtmlScanner.ScanNextToken(ref sourceScanState);

            //Assert
            token.HtmlElementType.Should().Be(expected, $"for [{token.TokenType}] : \"{token.NewValueStr()}\"");
        }

        [Test]
        public void BlockLevelHappyPathTests()
        {
            //Act
            var sourceScanState = new SourceScanState("<x><z><a/></z></x>".AsMemory());

            //Assert
            //<x> Open token should be level 0
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);

            //<z> Open token should be level 1
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(1);

            //<a/> Open token should be level 1
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(2);

            //</z> Open token should be level 1
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(1);

            //</x> Open token should be level 0
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);
        }

        [Test]
        public void BlockLevelInvalidHtmlTests()
        {
            //Act
            var sourceScanState = new SourceScanState("<x><a/></b></x>".AsMemory());

            //Assert
            //<x> Open token should be level 0
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);

            //<a/> Open token should be level 1
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(1);

            //</b> Open token should be level 1
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should().Be(0);

            //</x> Open token should be level 0
            SimpleHtmlScanner.ScanNextToken(ref sourceScanState).BlockLevel.Should()
                             .Be(-1); //think about what to do with this, could cap it to 0
        }

        [TestCase("<p>", HtmlElementType.P)]
        [TestCase("<ul>", HtmlElementType.Ul)]
        [TestCase("<ol>", HtmlElementType.Ol)]
        [TestCase("<li>", HtmlElementType.Li)]
        [TestCase("<p/>", HtmlElementType.P)]
        [TestCase("<  p  />", HtmlElementType.P)]
        [TestCase("</  p  >", HtmlElementType.P)]
        [TestCase("</p>", HtmlElementType.P)]
        [TestCase("<a>", HtmlElementType.Other)]
        [TestCase("<script>", HtmlElementType.Script)]
        [TestCase("</script>", HtmlElementType.Script)]
        [TestCase("<script/>", HtmlElementType.Script)]
        [TestCase("<style>", HtmlElementType.Style)]
        [TestCase("</style>", HtmlElementType.Style)]
        public void ParseToElementTypeTests(string input, HtmlElementType expected)
        {
            HtmlTagParser.Parse(input).Should().Be(expected);
        }
    }
}