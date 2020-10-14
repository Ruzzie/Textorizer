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

For more examples see the [testsuite](https://github.com/Ruzzie/Textorizer/blob/b9efa0fbff6d213cf56082b9fdd2f168cbfe8fb2/source/Textorizer.UnitTests/HtmlTextorizerTests.cs#L9) 

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