namespace TextFit;

/// <summary>
/// Result of <see cref="TextFitter.Fit(string, float, float, FitOptions)"/>: chosen font size,
/// wrapped lines, and helpers for converting line indices into rendering positions.
/// </summary>
public sealed class FitResult
{
    /// <summary>The font size the fitter selected.</summary>
    public float FontSize { get; init; }

    /// <summary>Distance between the baselines of consecutive lines (<c>FontSize &#215; LineSpacingFactor</c>).</summary>
    public float Leading { get; init; }

    /// <summary>The inner margin used between the text and the box edges.</summary>
    public float Margin { get; init; }

    /// <summary>The wrapped text, one entry per line. Empty paragraphs become empty strings.</summary>
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Yields each line with a position assuming a TOP-LEFT coordinate origin
    /// (Y increases downward — the convention used by most screen-space APIs).
    /// </summary>
    /// <returns>
    /// Tuples where <c>X</c> is the left edge of the line and <c>Y</c> is the TOP of the
    /// line's notional box. Add <see cref="FontSize"/> to <c>Y</c> to reach the baseline if
    /// your renderer expects baseline coordinates.
    /// </returns>
    public IEnumerable<(string Text, float X, float Y)> EnumerateTopLeft()
    {
        return Lines.Select((t, i) => (t, Margin, Margin + i * Leading));
    }

    /// <summary>
    /// Yields each line with a BASELINE position assuming a BOTTOM-LEFT coordinate origin
    /// (Y increases upward — the convention used by PDF).
    /// </summary>
    /// <param name="boxHeight">The same <c>boxHeight</c> you passed to <see cref="TextFitter.Fit(string, float, float, FitOptions)"/>.</param>
    /// <returns>
    /// Tuples where <c>X</c> is the left edge of the line and <c>Y</c> is the BASELINE of the
    /// line, measured from the bottom of the box.
    /// </returns>
    public IEnumerable<(string Text, float X, float Y)> EnumerateBaselineBottomLeft(float boxHeight)
    {
        for (var i = 0; i < Lines.Count; i++)
        {
            var topFromTop = Margin + i * Leading;
            var baselineFromBottom = boxHeight - topFromTop - FontSize;
            yield return (Lines[i], Margin, baselineFromBottom);
        }
    }
}