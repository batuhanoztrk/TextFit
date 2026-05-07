namespace TextFit;

using System.Text;

/// <summary>
/// Fits a piece of (possibly multi-line) text into a fixed bounding box by word-wrapping it
/// and selecting the largest font size that still fits.
/// </summary>
/// <remarks>
/// <para>
/// The fitter is renderer-agnostic — supply an <see cref="ITextMeasurer"/> implementation that
/// knows how to measure text in your font of choice, and you'll get back a <see cref="FitResult"/>
/// describing how the text should be drawn.
/// </para>
/// <para>
/// Instances are inexpensive to allocate and safe to reuse. Thread safety follows that of the
/// underlying <see cref="ITextMeasurer"/>.
/// </para>
/// </remarks>
public sealed class TextFitter
{
    // Splitting on (char[])null would also split on any whitespace, but allocating a
    // shared, explicit array clarifies the intent and side-steps overload-resolution
    // surprises with nullable reference types.
    private static readonly char[] WordSeparators = { ' ', '\t' };

    private readonly ITextMeasurer _measurer;

    /// <summary>Creates a new fitter that delegates measurement to <paramref name="measurer"/>.</summary>
    /// <param name="measurer">The text measurer to use. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="measurer"/> is <c>null</c>.</exception>
    public TextFitter(ITextMeasurer measurer)
    {
        _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
    }

    /// <summary>
    /// Fits <paramref name="text"/> into a box of <paramref name="boxWidth"/> &#215; <paramref name="boxHeight"/>.
    /// </summary>
    /// <param name="text">
    /// The text to lay out. Newline characters (<c>\n</c> or <c>\r\n</c>) are honored as
    /// paragraph breaks; empty paragraphs render as a blank line.
    /// </param>
    /// <param name="boxWidth">The width of the box, in the same units as the font size.</param>
    /// <param name="boxHeight">The height of the box, in the same units as the font size.</param>
    /// <param name="options">Optional tuning. When <c>null</c>, sensible defaults are used.</param>
    /// <returns>A <see cref="FitResult"/> with the chosen font size and wrapped lines.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="text"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="boxWidth"/> or <paramref name="boxHeight"/> is non-positive.</exception>
    public FitResult Fit(string text, float boxWidth, float boxHeight, FitOptions? options = null)
    {
        if (boxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boxWidth), "Must be positive.");
        if (boxHeight <= 0) throw new ArgumentOutOfRangeException(nameof(boxHeight), "Must be positive.");

        options ??= new FitOptions();

        var margin = options.Margin >= 0
            ? options.Margin
            : Math.Max(2f, Math.Min(boxWidth, boxHeight) * 0.08f);

        var maxW = Math.Max(1f, boxWidth - 2f * margin);
        var maxH = Math.Max(1f, boxHeight - 2f * margin);

        var minFont = options.MinFontSize;
        var maxFont = options.MaxFontSize;
        if (options.MaxFontSizeBoxHeightFactor.HasValue)
        {
            var heightCapped = Math.Max(12f, boxHeight * options.MaxFontSizeBoxHeightFactor.Value);
            maxFont = Math.Min(maxFont, heightCapped);
        }

        if (maxFont < minFont) maxFont = minFont;

        var fit = FindBestFit(text, maxW, maxH, minFont, maxFont, options);

        return new FitResult
        {
            FontSize = fit.FontSize,
            Leading = fit.Leading,
            Lines = fit.Lines,
            Margin = margin,
        };
    }

    // ---- internal mechanics --------------------------------------------------------------

    // Binary-searches the [minFontSize, maxFontSize] range for the largest size whose
    // wrapped output fits in (maxWidth, maxHeight). Falls back to minFontSize if nothing
    // fits — and as an absolute last resort returns wrapped-at-min even if that overflows
    // (the caller may visually clip; we don't throw to avoid surprising the renderer).
    private (float FontSize, float Leading, IReadOnlyList<string> Lines) FindBestFit(
        string text, float maxWidth, float maxHeight, float minFontSize, float maxFontSize, FitOptions options)
    {
        var lo = minFontSize;
        var hi = maxFontSize;

        (float FontSize, float Leading, IReadOnlyList<string> Lines)? best = null;

        for (var i = 0; i < options.BinarySearchIterations; i++)
        {
            var mid = (lo + hi) / 2f;

            var candidate = TryFit(text, maxWidth, maxHeight, mid, options.LineSpacingFactor);

            if (candidate.HasValue)
            {
                best = candidate.Value;
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        if (best.HasValue) return best.Value;

        var atMin = TryFit(text, maxWidth, maxHeight, minFontSize, options.LineSpacingFactor);
        if (atMin.HasValue) return atMin.Value;

        // Even the minimum overflows — return wrapped-at-min so the caller has *something*.
        var fallbackLines = WrapByWidth(text, minFontSize, maxWidth);
        return (minFontSize, minFontSize * options.LineSpacingFactor, fallbackLines);
    }

    // Tests whether `text` wrapped at `fontSize` fits in the (maxWidth, maxHeight) bounds.
    // Returns null when it doesn't (height overflow OR a single token wider than maxWidth).
    private (float FontSize, float Leading, IReadOnlyList<string> Lines)? TryFit(
        string text, float maxWidth, float maxHeight, float fontSize, float lineSpacingFactor)
    {
        var lines = WrapByWidth(text, fontSize, maxWidth);
        var leading = fontSize * lineSpacingFactor;
        var totalH = lines.Count * leading;

        if (totalH > maxHeight) return null;

        // Greedy wrapping can leave a single overlong word on its own line. Reject this
        // candidate, so the binary search picks a smaller size instead of mid-word breaks.
        if (lines.Any(t => _measurer.MeasureWidth(t, fontSize) > maxWidth + 0.01f))
        {
            return null;
        }

        return (fontSize, leading, lines);
    }

    // Greedy word-wrap. Splits text by line, then by whitespace, then packs words into lines
    // up to maxWidth. Empty paragraphs produce empty lines (preserving paragraph spacing).
    private IReadOnlyList<string> WrapByWidth(string text, float fontSize, float maxWidth)
    {
        var output = new List<string>();
        var paragraphs = text.Replace("\r", string.Empty).Split('\n');

        foreach (var rawParagraph in paragraphs)
        {
            var p = rawParagraph.Trim();

            if (p.Length == 0)
            {
                output.Add(string.Empty);
                continue;
            }

            var words = p.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();

            foreach (var word in words)
            {
                if (line.Length == 0)
                {
                    line.Append(word);
                }
                else
                {
                    // Tentatively appended-with-space form. Allocates per word; acceptable for
                    // typical inputs and keeps the code straightforward.
                    var candidate = line + " " + word;
                    if (_measurer.MeasureWidth(candidate, fontSize) <= maxWidth)
                    {
                        line.Append(' ').Append(word);
                    }
                    else
                    {
                        output.Add(line.ToString());
                        line.Clear();
                        line.Append(word);
                    }
                }
            }

            if (line.Length > 0)
            {
                output.Add(line.ToString());
            }
        }

        return output;
    }
}