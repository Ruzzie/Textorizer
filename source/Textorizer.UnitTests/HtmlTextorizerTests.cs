using System;
using System.IO;
using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace Textorizer.UnitTests
{
    [TestFixture]
    public class HtmlTextorizerTests
    {
        [TestCase("<p>plain text</p>", "\nplain text\n")]
        [TestCase("<div>plain text with no html</div>", "plain text with no html")]
        [TestCase("<div/>plain text with no html", "plain text with no html")]
        [TestCase("1. plain text with no html", "1. plain text with no html")]
        [TestCase("2. plain text <div></div>with no html", "2. plain text with no html")]
        [TestCase("3. plain text <div><p>Hello Hoi</div>with no html", "3. plain text \nHello Hoiwith no html")]
        [TestCase("4. plain text <div><p>Hello Hoi</div> with no html", "4. plain text \nHello Hoi with no html")]
        [TestCase("<p>hoi<p>ook</div></P><p>deef</p>", "\nhoi\nook\n\ndeef\n")]
        [TestCase("<p>hoi<p>ook</p>la</p>", "\nhoi\nook\nla\n")]
        [TestCase("<p><ul><li>item 1</li><li>item 2</li></ul></p>", "\n\n\n\t- item 1\n\t- item 2\n\n")]
        [TestCase("1. aaa<", "1. aaa<")]
        [TestCase("2. aaa<a", "2. aaa<a")]
        [TestCase("3. aaa<a<", "3. aaa<a<")]
        [TestCase("4. a<a<a", "4. a<a<a")]
        [TestCase("5. aaa<a<p>", "5. aaa<a\n")]
        [TestCase("aaa<1<p>", "aaa<1\n")]
        [TestCase("pre <br>post", "pre \npost")]
        [TestCase("pre <br/>post", "pre \npost")]
        [TestCase("pre <br>post</br>", "pre \npost")]
        [TestCase("pre <hr>post", "pre \npost")]
        [TestCase("pre&nbsp;post", "pre post")]
        [TestCase("pre&#160;post", "pre post")]
        [TestCase("A this is no html: 1 < 3 and 1 > 3", "A this is no html: 1 < 3 and 1 > 3")]
        [TestCase("B this is html: <p> 1 < 3 and 1 > 3</p>", "B this is html: \n1 < 3 and 1 > 3\n")]
        [TestCase("C this is html: <p   > 1 < 3 and 1 > 3</p>", "C this is html: \n1 < 3 and 1 > 3\n")]
        [TestCase("1.<p class=\"c n\\<\">a</p>", "1.\na\n")]
        [TestCase("2.<p class=\"c n\\<\">b<br/>a</p>", "2.\nb\na\n")]
        [TestCase("1. hoi<script>var x = '<>';window.close();</script>", "1. hoi")]
        [TestCase("2. hoi<script>var x = '\"<>';window.close();</script>", "2. hoi")]
        [TestCase("3. hoi<script>var x = \"'<>'\";window.close();</script>", "3. hoi")]
        [TestCase("4. hoi<script>var x = \"'<>'\";window.close();</s>", "4. hoi")]
        [TestCase("5. hoi<script>var x = \" '<>' \";window.close();<p>a<p/>", "5. hoi\na\n")]
        [TestCase("6. hoi<script>var x = \" '<>' \";if(x < 1) window.close();<p>a<p/>", "6. hoi\na\n")]
        [TestCase("7. hoi<script>var x = \" '<>' \";if(x > 1) window.close();<p>a<p/>", "7. hoi\na\n")]
        [TestCase("1. hoi<style>#\\~\\!\\@\\$\\%\\^\\&\\*\\(\\)\\_\\+-\\=\\,\\.\\/\\'\\;\\:\\\"\\?\\>\\<\\[\\]\\\\\\{\\}\\|\\`\\#{</style><p>a</p>",
                  "1. hoi\na\n")]
        [TestCase("2. hoi<style>.c{'a:1'};</style><p>a</p>", "2. hoi\na\n")]
        [TestCase("3. hoi<style>.c < p{'a:1'};</style><p>a</p>", "3. hoi\na\n")]
        [TestCase("4. hoi<style>.c > p{'a:1'};</style><p>a</p>", "4. hoi\na\n")]
        [TestCase("5. hoi<style>.c > p{'a:1'};</s> a", "5. hoi a")]
        [TestCase("6. hoi<style>.c > p{'a:1'};</s><p>a</p>", "6. hoi\n\n")]
        [TestCase("1. X<b>-bld</b>", "1. X-bld")]
        [TestCase("1. <p>Hello</p>    How are you? ", "1. \nHello\nHow are you? ")]
        [TestCase("li 1. <li>Y   </li>", "li 1. \n\t- Y ")]
        [TestCase("li 2. <li>   X</li>", "li 2. \n\t- X")]
        [TestCase("li 3. <li>X <b>bld</bld></li>", "li 3. \n\t- X bld")]

        [TestCase("ul 1. <ul><li>lvl 1<ul><li>lvl2</li></ul></li></ul>", "ul 1. \n\n\t- lvl 1\n\t\t- lvl2\n")]

        [TestCase("1. <b>&lt;word&gt;</b>", "1. <word>")]
        [TestCase("2. <b>&ltword&gt</b>", "2. &ltword&gt")]
        [TestCase("1. <p>hi</p>\r\n\r\n\r\n        ok?", "1. \nhi\nok?")]
        [TestCase("2. <b>hi</b>\r\n\r\n\r\n        ok?", "2. hi ok?")]
        [TestCase("3. <div><b>hi</b>\r\n\r\n\r\n        ok?</div>", "3. hi ok?")]
        [TestCase("4. <div>a <a>hi</a>  ok?</div>", "4. a hi ok?")]
        [TestCase("pre 1. <pre>\n code \r\n\r\n\r\n        ok?</pre>", "pre 1.  code \r\n\r\n\r\n        ok?")]
        [TestCase("pre 2. <pre>\n <span>code \r\n\r\n\r\n</span>        ok?</pre>",
                  "pre 2.  code \r\n\r\n\r\n        ok?")]
        [TestCase("pre 3. <pre>\n<pre> <span>code \r\n\r\n\r\n</span></pre>        ok?</pre>",
                  "pre 3.  code \r\n\r\n\r\n        ok?")]

        [TestCase("1. invalid entity <b>&hellip<i>word</i></b>", "1. invalid entity &hellipword")]
        [TestCase("2. invalid entity <b>&hellip    <i>word</i></b>", "2. invalid entity &hellip word")]
        [TestCase("3. invalid entity <b>&hellip;    <i>&hellipword</i></b>", "3. invalid entity … &hellipword")]
        public void SmokeTests(string htmlInput, string expectedOutput)
        {
            //Act
            string result = Textorize.HtmlToPlainText(htmlInput);

            //Assert
            result.Should().Be(expectedOutput);
        }
        [TestCase("\nhello \n")]
        [TestCase("\nhello  hoi \n")]
        [TestCase(" \nhello hoi \n ")]
        [TestCase(" \nhello        hoi \n ")]
        [TestCase("hello ik ben >  3  & 7 < 3      hoi \n")]
        [TestCase("hello<p> ik ben >  3  & 7 < 3      hoi \n")]
        //[TestCase(" \nhello&#160;hoi \n ")]
        public void TextorizeTwiceOverPlainTextShouldHaveEqualResults(string input)
        {
            var first  = Textorize.HtmlToPlainText(input);
            var second = Textorize.HtmlToPlainText(first);

            first.Should().BeEquivalentTo(second);

            Textorize.HtmlToPlainText(WebUtility.HtmlEncode(second)).Should().BeEquivalentTo(first);
        }


        [FsCheck.NUnit.Property]
        public void PropertyTestsShouldNotThrowException(string inputA, string inputB)
        {
            //Act
            Textorize.HtmlToPlainText(inputA + inputB);
        }

        [Test]
        public void HtmlFileShouldGiveSomeReasonableResult()
        {
            //Arrange
            var testFilename = Path.Join(TestContext.CurrentContext.TestDirectory, "testdata", "input_html_01.html");

            //Act
            var result = Textorize.HtmlToPlainText(File.ReadAllText(testFilename));
            File.WriteAllText(Path.Join(TestContext.CurrentContext.TestDirectory, "input_html_01-sanitized.txt"),
                              result);

            //Assert
            Console.Write(result);
        }

        [Test]
        public void HtmlFileTwiceShouldBeEquivalent()
        {
            //Arrange
            var testFilename = Path.Join(TestContext.CurrentContext.TestDirectory, "testdata", "input_html_01.html");

            //Act
            var first  = Textorize.HtmlToPlainText(File.ReadAllText(testFilename));
            var second = Textorize.HtmlToPlainText(WebUtility.HtmlEncode(first));
            File.WriteAllText(Path.Join(TestContext.CurrentContext.TestDirectory, "input_html_01-2-sanitized.txt"),
                              second);

            //Assert Textorize(input) == Textorize(HtmlEncode(Textorize(input)))
            first.Should().BeEquivalentTo(second);
        }
    }
}