# Textorizer
[![Build status](https://ci.appveyor.com/api/projects/status/rsvw865mti05iulv?svg=true)](https://ci.appveyor.com/project/Ruzzie/textorizer)
![Nuget](https://img.shields.io/nuget/v/Textorizer)

Sanitize and 'clean' html for safe consumption in a plain text format.

```csharp
  var plainText = Textorize.HtmlToPlainText("<span>I contain html</span><p>convert me</p>");
  //  plaintext = "I contain html\nconvert me\n"  
```
Converts html input to a safe plain text representation without html. 
Content in Style and Script tags are completely removed, html entity characters are explicitly converted to their unicode characters.
Invalid html is handled best effort for a reasonable equivalent plain text output.

Keep in mind the following equivalence:
        
    Textorize(input) == Textorize(HtmlEncode(Textorize(input)))

For more examples see the [testsuite](https://github.com/Ruzzie/Textorizer/blob/ae0577ed07f930759a1796bb877cd31884fe6709/source/Textorizer.UnitTests/HtmlTextorizerTests.cs#L12) 

## Install

### Package Manager Console

```
PM> Install-Package Textorizer
```

### .NET CLI Console

```
> dotnet add package Textorizer
```

## License

Dual licensed

MIT

https://opensource.org/licenses/MIT

Unlicense

https://opensource.org/licenses/Unlicense
