using System;
using FluentAssertions;
using NUnit.Framework;

namespace Textorizer.UnitTests;

[TestFixture]
public class SourceScanStateTests
{
    [Test]
    public void IsAtEndReturnsTrueWhenCurrentIndexAtEnd()
    {
        //Arrange
        var input     = "123".AsMemory();
        var scanState = new SourceScanState(input);

        //Act
        scanState.Advance();
        scanState.Advance();
        scanState.Advance();

        //Assert
        scanState.IsAtEnd().Should().BeTrue();
    }

    [Test]
    public void AdvanceReturnsCharacters()
    {
        //Arrange
        var scanState = new SourceScanState("123".AsMemory());

        //Act & Assert
        scanState.Advance().Should().Be('1');
        scanState.Advance().Should().Be('2');
        scanState.Advance().Should().Be('3');
    }

    [Test]
    public void AdvanceReturnsNullCharWhenAtEnd()
    {
        //Arrange
        var input     = "123";
        var scanState = new SourceScanState(input.AsMemory());

        scanState.Advance();
        scanState.Advance();
        scanState.Advance();

        //Act & Assert
        scanState.Advance().Should().Be('\0');
    }

    [Test]
    public void LookAheadReturnsStuff()
    {
        //Arrange
        var input     = "123";
        var scanState = new SourceScanState(input.AsMemory());

        //Act
        scanState.LookAhead(2).ToArray().Should().BeEquivalentTo(new[] {'1', '2'});
    }

    [Test]
    public void LookAheadTillEndReturnsStuff()
    {
        //Arrange
        var input     = "123";
        var scanState = new SourceScanState(input.AsMemory());
        scanState.Advance();

        //Act
        scanState.LookAhead(2).ToArray().Should().BeEquivalentTo(new[] {'2', '3'});
    }

    [Test]
    public void LookAheadWhenAtEndReturnsEmpty()
    {
        //Arrange
        var input     = "123";
        var scanState = new SourceScanState(input.AsMemory());
        scanState.Advance();
        scanState.Advance();
        scanState.Advance();

        //Act & Assert
        scanState.LookAhead(2).IsEmpty.Should().BeTrue();
    }

    [Test]
    public void LookAheadReturnsNothingWhenOutOfRange()
    {
        //Arrange
        var input     = "123";
        var scanState = new SourceScanState(input.AsMemory());

        //Act
        scanState.LookAhead(4).IsEmpty.Should().BeTrue();
    }

    [Test]
    public void PeekNextReturnsNextChar()
    {
        //Arrange
        var input = "123";

        var scanState = new SourceScanState(input.AsMemory());
        //Advance once
        scanState.Advance();

        //Act
        scanState.PeekNext().Should().Be('2');
    }

    [Test]
    public void PeekNextAtEndReturnsNullChar()
    {
        //Arrange
        var input = "1";

        var scanState = new SourceScanState(input.AsMemory());
        //Advance once
        scanState.Advance();

        //Act
        scanState.PeekNext().Should().Be('\0');
    }
}