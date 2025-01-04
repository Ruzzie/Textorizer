using System;
using System.Runtime.CompilerServices;

namespace Textorizer;

internal ref struct SourceScanState
{
    public int  CurrentPos => _currentPos;
    public int  StartIndex;
    public int  BlockLevel;
    public bool IsInTag;

    public readonly ReadOnlyMemory<char> SourceData;
    public readonly ReadOnlySpan<char>   SourceDataSpan;

    private readonly int _sourceDataLength;
    private          int _currentPos;

    public SourceScanState(in ReadOnlyMemory<char> input)
    {
        _currentPos       = 0;
        _sourceDataLength = input.Length;
        SourceData        = input;
        SourceDataSpan    = SourceData.Span;
        BlockLevel        = 0;
        StartIndex        = 0;
        IsInTag           = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAtEnd()
    {
        return _currentPos >= _sourceDataLength;
    }

    public char Advance()
    {
        if (!IsAtEnd())
        {
            _currentPos++;
            return SourceDataSpan[_currentPos - 1];
        }

        return '\0'; //NULL CHAR
    }

    public char BackTrack(int toPosition)
    {
        if (toPosition > _currentPos)
            return '\0';
        if (toPosition < StartIndex)
            return '\0';
        if(toPosition >= _sourceDataLength) // at end
            return '\0';

        _currentPos = toPosition;

        return SourceDataSpan[toPosition];
    }

    public void Advance(int count)
    {
        if (!IsAtEnd())
        {
            var newIndex = _currentPos + count;
            if (newIndex <= _sourceDataLength)
            {
                _currentPos = newIndex;
            }
        }
    }

    public ReadOnlySpan<char> LookAhead(int count)
    {
        if ((_currentPos + count) <= _sourceDataLength)
        {
            return SourceDataSpan.Slice(_currentPos, count);
        }

        return ReadOnlySpan<char>.Empty;
    }

    public char PeekNext()
    {
        if ((_currentPos + 1) <= _sourceDataLength)
        {
            return SourceDataSpan[_currentPos];
        }

        return '\0';
    }

    public char PeekNextUnsafe()
    {
        return SourceDataSpan[_currentPos];
    }
}