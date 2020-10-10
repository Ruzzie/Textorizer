using System;
using System.IO;
using DCrafting.Textorize.Html;
using FluentAssertions;
using NUnit.Framework;

namespace DCrafting.Textorize.UnitTests
{
    [TestFixture]
    public class HtmlSanitizerTests
    {
        private readonly HtmlTextorizer _textorizer = new HtmlTextorizer(new HtmlToPlainTextWriter());

        [TestCase("<p>plain text</p>", "\nplain text\n")]
        [TestCase("plain text with no html", "plain text with no html")]
        [TestCase("<div>plain text with no html</div>", "plain text with no html")]
        [TestCase("<div/>plain text with no html", "plain text with no html")]
        [TestCase("plain text <div></div>with no html", "plain text with no html")]
        [TestCase("plain text <div><p>Hello Hoi</div>with no html", "plain text \nHello Hoi\nwith no html")]
        [TestCase("<p>hoi<p>ook</div></P><p>deef</p>", "\nhoi\nook\n\n\ndeef\n")]
        [TestCase("<p>hoi<p>ook</p>la</p>", "\nhoi\nook\nla\n")]
        [TestCase("<p><ul><li>item 1</li><li>item 2</li></ul></p>", "\n\n\t- item 1\n\t- item 2\n\n")]
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
        [TestCase("1. hoi<script>var x = '<>';window.close();</script>","1. hoi")]
        [TestCase("2. hoi<script>var x = '\"<>';window.close();</script>","2. hoi")]
        [TestCase("3. hoi<script>var x = \"'<>'\";window.close();</script>","3. hoi")]
        [TestCase("4. hoi<script>var x = \"'<>'\";window.close();</s>","4. hoi")]
        [TestCase("5. hoi<script>var x = \" '<>' \";window.close();<p>a<p/>","5. hoi\na\n")]
        [TestCase("1. hoi<style>#\\~\\!\\@\\$\\%\\^\\&\\*\\(\\)\\_\\+-\\=\\,\\.\\/\\'\\;\\:\\\"\\?\\>\\<\\[\\]\\\\\\{\\}\\|\\`\\#{</style><p>a</p>","1. hoi\na\n")]
        [TestCase("2. hoi<style>.c{'a:1'};</style><p>a</p>","2. hoi\na\n")]

        [TestCase("1. X<b>-bld</b>","1. X-bld")]


        [TestCase("1. <p>Hello</p>    How are you? ","1. \nHello\nHow are you? ")]
        [TestCase("2. <li>Y   </li>","2. \t- Y   \n")]
        [TestCase("3. <li>   X</li>","3. \t- X\n")]
        [TestCase("4. <li>X <b>bld</bld></li>","4. \t- X bld\n")]


        [TestCase("1. <b>&lt;word&gt;</b>","1. <word>")]
        public void SmokeTests(string possibleHtmlStringInput, string expectedOutput)
        {
            //Act
            string result = _textorizer.Textorize(possibleHtmlStringInput);

            //Assert
            result.Should().Be(expectedOutput);
        }

        [FsCheck.NUnit.Property]
        public void PropertyTestsShouldNotThrowException(string inputA, string inputB)
        {
            //Act
            _textorizer.Textorize(inputA + inputB);
        }

        [Test]
        public void HtmlFileShouldGiveSomeReasonableResult()
        {
            //Arrange
            var testFilename = Path.Join(TestContext.CurrentContext.TestDirectory, "input_html_01.html");
            //Act
            var result = _textorizer.Textorize(File.ReadAllText(testFilename));
            File.WriteAllText( Path.Join(TestContext.CurrentContext.TestDirectory, "input_html_01-sanitized.txt" ),result);
            //Assert
            Console.Write(result);
        }
    }
}