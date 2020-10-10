using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DCrafting.Textorize.Html;

namespace DCrafting.Textorize.Benchmarks
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
        //private DCrafting.Old.Textorize.ITextorizer _oldHtmlSanitizer = new  DCrafting.Old.Textorize.Html.HtmlTextorizer(new DCrafting.Old.Textorize.Html.HtmlToPlainTextWriter());

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
        public string StripHtmlOptimized()
        {
            return _optHtmlTextorizer.Textorize(Data);
        }
/*

        [Benchmark]
        public string StripHtmlOld()
        {
            return _oldHtmlSanitizer.Textorize(Data);
        }*/

        [Benchmark]
        public string NewReplace()
        {
            return HtmlToPlainTextWriter.ReplaceHtmlWhiteSpacesNew(Data);
        }
    }
}