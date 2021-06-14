using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Textorizer;
using Textorizer.Html;

namespace Textorizer.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }

    [MemoryDiagnoser]
    [HtmlExporter, RPlotExporter]
    public class StringReplaceBenchmarks
    {
        [ParamsSource(nameof(ValuesForData))]
        public string Data { get; set; }

        private readonly ITextorizer _optHtmlTextorizer = new HtmlTextorizer(new HtmlToPlainTextWriter());

        public IEnumerable<string> ValuesForData => new[]
        {
            " short string<br/>  \r\n  "
            ,
            @"
            
                Languages
            
            
            
                
                																																										- Português
                   - Русский
                   - Svenska
                   - தமிழ்
                   - Türkçe
                   - Українська
                   - Tiếng Việt
                - 中文
<br/>

            Edit links
            
        

    



    
            "
        };

        [Benchmark(Baseline = true)]
        public string ClassicReplace()
        {
            return HtmlToPlainTextWriter.ReplaceHtmlWhiteSpacesClassic(Data);
        }

        [Benchmark]
        public string Textorize()
        {
            return _optHtmlTextorizer.Textorize(Data);
        }

        [Benchmark]
        public string ReduceHtmlWhiteSpaces()
        {
            return HtmlToPlainTextWriter.ReduceHtmlWhiteSpaces(Data);
        }
    }
}